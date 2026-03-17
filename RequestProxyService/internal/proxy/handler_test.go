package proxy

import (
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"

	"request-proxy-service/internal/config"

	"github.com/gin-gonic/gin"
)

func setupRouter(handler *Handler) *gin.Engine {
	gin.SetMode(gin.TestMode)
	router := gin.New()
	router.POST("/api/proxy/execute", handler.Execute)
	return router
}

func TestExecute_ServiceNotDefined(t *testing.T) {
	t.Parallel()

	handler := NewHandler(map[string]config.ServiceDefinition{})
	router := setupRouter(handler)

	body := `{"service":"unknown","method":"GET","path":"/x"}`
	req := httptest.NewRequest(http.MethodPost, "/api/proxy/execute", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	resp := httptest.NewRecorder()

	router.ServeHTTP(resp, req)

	if resp.Code != http.StatusNotFound {
		t.Fatalf("expected 404, got %d", resp.Code)
	}
}

func TestExecute_MethodNotAllowed(t *testing.T) {
	t.Parallel()

	handler := NewHandler(map[string]config.ServiceDefinition{
		"svc": {
			ID:             "svc",
			BaseURL:        "https://example.com",
			AllowedMethods: []string{"GET"},
		},
	})
	router := setupRouter(handler)

	body := `{"service":"svc","method":"POST","path":"/x"}`
	req := httptest.NewRequest(http.MethodPost, "/api/proxy/execute", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	resp := httptest.NewRecorder()

	router.ServeHTTP(resp, req)

	if resp.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", resp.Code)
	}
}

func TestExecute_SuccessRawProxy(t *testing.T) {
	t.Parallel()

	upstream := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/search" {
			t.Fatalf("expected /search path, got %s", r.URL.Path)
		}
		if r.URL.Query().Get("q") != "cars" {
			t.Fatalf("expected q=cars, got %s", r.URL.Query().Get("q"))
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{"ok":true}`))
	}))
	defer upstream.Close()

	handler := NewHandler(map[string]config.ServiceDefinition{
		"svc": {
			ID:             "svc",
			BaseURL:        upstream.URL,
			AllowedMethods: []string{"GET"},
		},
	})
	router := setupRouter(handler)

	body := `{"service":"svc","method":"GET","path":"/search","query":{"q":"cars"}}`
	req := httptest.NewRequest(http.MethodPost, "/api/proxy/execute", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	resp := httptest.NewRecorder()

	router.ServeHTTP(resp, req)

	if resp.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d, body=%s", resp.Code, resp.Body.String())
	}
	if contentType := resp.Header().Get("Content-Type"); !strings.Contains(contentType, "application/json") {
		t.Fatalf("expected application/json content type, got %s", contentType)
	}
	if strings.TrimSpace(resp.Body.String()) != `{"ok":true}` {
		t.Fatalf("unexpected body: %s", resp.Body.String())
	}
}
