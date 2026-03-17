package proxy

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"os"
	"regexp"
	"strings"
	"time"

	"request-proxy-service/internal/config"

	"github.com/gin-gonic/gin"
)

var envPlaceholderRegex = regexp.MustCompile(`\$\{([A-Z0-9_]+)\}`)

type Handler struct {
	services   map[string]config.ServiceDefinition
	httpClient *http.Client
}

func NewHandler(services map[string]config.ServiceDefinition) *Handler {
	return &Handler{
		services: services,
		httpClient: &http.Client{
			Timeout: 20 * time.Second,
		},
	}
}

// Execute proxies a configured upstream request.
// @Summary Execute configured upstream service request
// @Description Executes a request against a whitelisted service from config/services.json and returns raw upstream payload for 2xx responses.
// @Tags RequestProxyService
// @Accept json
// @Produce json
// @Produce plain
// @Produce xml
// @Param request body ExecuteRequest true "Proxy request definition"
// @Success 200 {string} string "Raw upstream response body"
// @Failure 400 {object} ErrorResponse
// @Failure 404 {object} ErrorResponse
// @Failure 405 {object} ErrorResponse
// @Failure 500 {object} ErrorResponse
// @Failure 502 {object} ErrorResponse
// @Router /api/proxy/execute [post]
func (h *Handler) Execute(c *gin.Context) {
	var req ExecuteRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, ErrorResponse{Error: "invalid request payload", Details: err.Error()})
		return
	}

	serviceDef, ok := h.services[req.Service]
	if !ok {
		c.JSON(http.StatusNotFound, ErrorResponse{Error: "service not defined", Service: req.Service})
		return
	}

	method := strings.ToUpper(req.Method)
	if !isMethodAllowed(method, serviceDef.AllowedMethods) {
		c.JSON(http.StatusMethodNotAllowed, ErrorResponse{
			Error:   "method not allowed for service",
			Service: req.Service,
			Details: fmt.Sprintf("allowed methods: %s", strings.Join(serviceDef.AllowedMethods, ",")),
		})
		return
	}

	targetURL, err := buildTargetURL(serviceDef, req.Path, req.Query)
	if err != nil {
		c.JSON(http.StatusBadRequest, ErrorResponse{Error: "invalid target path/query", Service: req.Service, Details: err.Error()})
		return
	}

	upstreamReq, err := buildUpstreamRequest(c.Request.Context(), method, targetURL, req.Body)
	if err != nil {
		c.JSON(http.StatusBadRequest, ErrorResponse{Error: "invalid body for request", Service: req.Service, Details: err.Error()})
		return
	}

	applyHeaders(upstreamReq, serviceDef.DefaultHeaders)
	applyHeaders(upstreamReq, req.Headers)

	upstreamResp, err := h.httpClient.Do(upstreamReq)
	if err != nil {
		c.JSON(http.StatusBadGateway, ErrorResponse{Error: "failed to execute upstream request", Service: req.Service, Details: err.Error()})
		return
	}
	defer upstreamResp.Body.Close()

	respBody, err := io.ReadAll(upstreamResp.Body)
	if err != nil {
		c.JSON(http.StatusBadGateway, ErrorResponse{Error: "failed to read upstream response", Service: req.Service, Details: err.Error()})
		return
	}

	if upstreamResp.StatusCode < 200 || upstreamResp.StatusCode >= 300 {
		c.JSON(http.StatusBadGateway, ErrorResponse{
			Error:   "upstream request failed",
			Service: req.Service,
			Status:  upstreamResp.StatusCode,
			Details: truncateForError(string(respBody), 500),
		})
		return
	}

	contentType := upstreamResp.Header.Get("Content-Type")
	if contentType == "" {
		contentType = "application/octet-stream"
	}

	c.Data(upstreamResp.StatusCode, contentType, respBody)
}

func buildUpstreamRequest(ctx context.Context, method, targetURL string, body any) (*http.Request, error) {
	if method == http.MethodGet || method == http.MethodDelete || method == http.MethodHead {
		return http.NewRequestWithContext(ctx, method, targetURL, nil)
	}

	if body == nil {
		return http.NewRequestWithContext(ctx, method, targetURL, nil)
	}

	payload, err := json.Marshal(body)
	if err != nil {
		return nil, err
	}

	return http.NewRequestWithContext(ctx, method, targetURL, bytes.NewReader(payload))
}

func applyHeaders(req *http.Request, headers map[string]string) {
	for key, value := range headers {
		resolved := resolveEnvPlaceholders(value)
		if resolved == "" {
			continue
		}
		req.Header.Set(key, resolved)
	}

	if req.Body != nil && req.Header.Get("Content-Type") == "" {
		req.Header.Set("Content-Type", "application/json")
	}
}

func buildTargetURL(serviceDef config.ServiceDefinition, requestPath string, query map[string]string) (string, error) {
	base, err := url.Parse(serviceDef.BaseURL)
	if err != nil {
		return "", err
	}

	if !strings.HasPrefix(requestPath, "/") {
		requestPath = "/" + requestPath
	}
	if strings.HasPrefix(requestPath, "//") {
		return "", fmt.Errorf("path cannot start with //")
	}

	rel, err := url.Parse(requestPath)
	if err != nil {
		return "", err
	}

	target := base.ResolveReference(rel)
	params := target.Query()

	for key, value := range serviceDef.DefaultQuery {
		params.Set(key, resolveEnvPlaceholders(value))
	}
	for key, value := range query {
		params.Set(key, value)
	}
	target.RawQuery = params.Encode()

	return target.String(), nil
}

func isMethodAllowed(method string, allowed []string) bool {
	for _, m := range allowed {
		if method == strings.ToUpper(m) {
			return true
		}
	}
	return false
}

func resolveEnvPlaceholders(value string) string {
	return envPlaceholderRegex.ReplaceAllStringFunc(value, func(token string) string {
		match := envPlaceholderRegex.FindStringSubmatch(token)
		if len(match) < 2 {
			return token
		}
		return os.Getenv(match[1])
	})
}

func truncateForError(value string, max int) string {
	if len(value) <= max {
		return value
	}
	return value[:max] + "..."
}
