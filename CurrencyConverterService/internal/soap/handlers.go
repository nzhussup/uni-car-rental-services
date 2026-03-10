package soap

import (
	"context"
	"encoding/xml"
	"errors"
	"io"
	"log/slog"
	"net/http"
	"os"

	"currency-converter-service/internal/service"
)

type converterService interface {
	GetExchangeRate(ctx context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error)
	ConvertAmount(ctx context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error)
	GetSupportedCurrencies(ctx context.Context) ([]string, error)
}

type Handler struct {
	converter converterService
	wsdlPath  string
	username  string
	password  string
	logger    *slog.Logger
}

func NewHandler(converter converterService, wsdlPath, username, password string, logger *slog.Logger) *Handler {
	if logger == nil {
		logger = slog.Default()
	}

	return &Handler{
		converter: converter,
		wsdlPath:  wsdlPath,
		username:  username,
		password:  password,
		logger:    logger,
	}
}

func (h *Handler) HandleSOAP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		w.WriteHeader(http.StatusMethodNotAllowed)
		return
	}

	if !h.checkBasicAuth(r) {
		w.Header().Set("WWW-Authenticate", `Basic realm="currency-converter"`)
		w.WriteHeader(http.StatusUnauthorized)
		return
	}

	body, err := io.ReadAll(io.LimitReader(r.Body, 1<<20))
	if err != nil {
		h.writeSOAPFault(w, http.StatusBadRequest, NewClientFault("failed to read SOAP request body"))
		return
	}

	var envelope requestEnvelope
	if err := xml.Unmarshal(body, &envelope); err != nil {
		h.writeSOAPFault(w, http.StatusBadRequest, NewClientFault("invalid SOAP XML"))
		return
	}

	switch {
	case envelope.Body.GetExchangeRateRequest != nil:
		h.handleGetExchangeRate(w, r, envelope.Body.GetExchangeRateRequest)
	case envelope.Body.ConvertAmountRequest != nil:
		h.handleConvertAmount(w, r, envelope.Body.ConvertAmountRequest)
	case envelope.Body.GetSupportedCurrenciesRequest != nil:
		h.handleGetSupportedCurrencies(w, r)
	default:
		h.writeSOAPFault(w, http.StatusBadRequest, NewClientFault("missing or unsupported SOAP operation"))
	}
}

func (h *Handler) HandleWSDL(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		w.WriteHeader(http.StatusMethodNotAllowed)
		return
	}

	wsdlContent, err := os.ReadFile(h.wsdlPath)
	if err != nil {
		h.logger.Error("failed to read WSDL file", "error", err)
		w.WriteHeader(http.StatusInternalServerError)
		_, _ = w.Write([]byte("failed to read WSDL"))
		return
	}

	w.Header().Set("Content-Type", "text/xml; charset=utf-8")
	_, _ = w.Write(wsdlContent)
}

func (h *Handler) HandleHealth(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		w.WriteHeader(http.StatusMethodNotAllowed)
		return
	}

	w.Header().Set("Content-Type", "text/plain; charset=utf-8")
	_, _ = w.Write([]byte("ok"))
}

func (h *Handler) handleGetExchangeRate(w http.ResponseWriter, r *http.Request, req *GetExchangeRateRequest) {
	result, err := h.converter.GetExchangeRate(r.Context(), req.FromCurrency, req.ToCurrency)
	if err != nil {
		h.handleServiceError(w, err)
		return
	}

	resp := GetExchangeRateResponse{
		Rate:           result.Rate,
		Source:         result.Source,
		BaseCurrency:   result.BaseCurrency,
		TargetCurrency: result.TargetCurrency,
	}

	h.writeSOAPResponse(w, resp)
}

func (h *Handler) handleConvertAmount(w http.ResponseWriter, r *http.Request, req *ConvertAmountRequest) {
	result, err := h.converter.ConvertAmount(r.Context(), req.Amount, req.FromCurrency, req.ToCurrency)
	if err != nil {
		h.handleServiceError(w, err)
		return
	}

	resp := ConvertAmountResponse{
		ConvertedAmount: result.ConvertedAmount,
		Rate:            result.Rate,
		Source:          result.Source,
		BaseCurrency:    result.BaseCurrency,
		TargetCurrency:  result.TargetCurrency,
	}

	h.writeSOAPResponse(w, resp)
}

func (h *Handler) handleGetSupportedCurrencies(w http.ResponseWriter, r *http.Request) {
	currencies, err := h.converter.GetSupportedCurrencies(r.Context())
	if err != nil {
		h.handleServiceError(w, err)
		return
	}

	resp := GetSupportedCurrenciesResponse{Currencies: currencies}
	h.writeSOAPResponse(w, resp)
}

func (h *Handler) handleServiceError(w http.ResponseWriter, err error) {
	h.logger.Error("service operation failed", "error", err)

	if service.IsClientError(err) {
		h.writeSOAPFault(w, http.StatusBadRequest, NewClientFault(err.Error()))
		return
	}

	if errors.Is(err, io.EOF) {
		h.writeSOAPFault(w, http.StatusBadRequest, NewClientFault("empty SOAP body"))
		return
	}

	h.writeSOAPFault(w, http.StatusInternalServerError, NewServerFault("internal service error"))
}

func (h *Handler) checkBasicAuth(r *http.Request) bool {
	username, password, ok := r.BasicAuth()
	if !ok {
		return false
	}

	return username == h.username && password == h.password
}

func (h *Handler) writeSOAPResponse(w http.ResponseWriter, payload any) {
	innerXML, err := xml.Marshal(payload)
	if err != nil {
		h.logger.Error("failed to marshal SOAP response", "error", err)
		h.writeSOAPFault(w, http.StatusInternalServerError, NewServerFault("failed to create SOAP response"))
		return
	}

	envelope := buildResponseEnvelope(innerXML)
	h.writeXMLEnvelope(w, http.StatusOK, envelope)
}

func (h *Handler) writeSOAPFault(w http.ResponseWriter, statusCode int, fault SOAPFault) {
	faultXML, err := xml.Marshal(fault)
	if err != nil {
		h.logger.Error("failed to marshal SOAP fault", "error", err)
		w.WriteHeader(http.StatusInternalServerError)
		_, _ = w.Write([]byte("failed to create SOAP fault"))
		return
	}

	envelope := buildResponseEnvelope(faultXML)
	h.writeXMLEnvelope(w, statusCode, envelope)
}

func (h *Handler) writeXMLEnvelope(w http.ResponseWriter, statusCode int, envelope responseEnvelope) {
	xmlBytes, err := xml.MarshalIndent(envelope, "", "  ")
	if err != nil {
		h.logger.Error("failed to marshal SOAP envelope", "error", err)
		w.WriteHeader(http.StatusInternalServerError)
		_, _ = w.Write([]byte("failed to create SOAP envelope"))
		return
	}

	w.Header().Set("Content-Type", "text/xml; charset=utf-8")
	w.WriteHeader(statusCode)
	_, _ = w.Write([]byte(xml.Header))
	_, _ = w.Write(xmlBytes)
}
