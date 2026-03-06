package soap

import (
	"context"
	"encoding/xml"
	"errors"
	"io"
	"log/slog"
	"math"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"currency-converter-service/internal/ecb"
	"currency-converter-service/internal/service"
)

type stubConverter struct {
	getRateFn      func(ctx context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error)
	convertFn      func(ctx context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error)
	getSupportedFn func(ctx context.Context) ([]string, error)
}

func (s stubConverter) GetExchangeRate(ctx context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error) {
	if s.getRateFn == nil {
		return service.ExchangeRateResult{}, errors.New("get rate not stubbed")
	}
	return s.getRateFn(ctx, fromCurrency, toCurrency)
}

func (s stubConverter) ConvertAmount(ctx context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error) {
	if s.convertFn == nil {
		return service.ConversionResult{}, errors.New("convert not stubbed")
	}
	return s.convertFn(ctx, amount, fromCurrency, toCurrency)
}

func (s stubConverter) GetSupportedCurrencies(ctx context.Context) ([]string, error) {
	if s.getSupportedFn == nil {
		return []string{"EUR", "USD"}, nil
	}
	return s.getSupportedFn(ctx)
}

type soapEnvelopeDecoded struct {
	XMLName xml.Name `xml:"Envelope"`
	Body    struct {
		Fault *struct {
			FaultCode   string `xml:"faultcode"`
			FaultString string `xml:"faultstring"`
			Detail      struct {
				Message string `xml:"Message"`
			} `xml:"detail"`
		} `xml:"Fault"`

		GetExchangeRateResponse *struct {
			Rate           float64 `xml:"Rate"`
			Source         string  `xml:"Source"`
			BaseCurrency   string  `xml:"BaseCurrency"`
			TargetCurrency string  `xml:"TargetCurrency"`
		} `xml:"GetExchangeRateResponse"`

		ConvertAmountResponse *struct {
			ConvertedAmount float64 `xml:"ConvertedAmount"`
			Rate            float64 `xml:"Rate"`
			Source          string  `xml:"Source"`
			BaseCurrency    string  `xml:"BaseCurrency"`
			TargetCurrency  string  `xml:"TargetCurrency"`
		} `xml:"ConvertAmountResponse"`

		GetSupportedCurrenciesResponse *struct {
			Currencies []string `xml:"Currencies>Currency"`
		} `xml:"GetSupportedCurrenciesResponse"`
	} `xml:"Body"`
}

func TestHandleSOAP_AuthAndMethodBehavior(t *testing.T) {
	t.Parallel()

	h := newTestHandler(t, stubConverter{
		getRateFn: func(context.Context, string, string) (service.ExchangeRateResult, error) {
			return service.ExchangeRateResult{Rate: 1.1, Source: "ECB", BaseCurrency: "EUR", TargetCurrency: "USD"}, nil
		},
	})

	reqBody := mustReadSOAPFixture(t, "soap-get-rate-valid.xml")

	tests := []struct {
		name              string
		method            string
		username          string
		password          string
		setAuth           bool
		wantStatus        int
		wantWWWAuthHeader bool
	}{
		{name: "missing auth", method: http.MethodPost, setAuth: false, wantStatus: http.StatusUnauthorized, wantWWWAuthHeader: true},
		{name: "wrong username", method: http.MethodPost, setAuth: true, username: "wrong", password: "admin", wantStatus: http.StatusUnauthorized, wantWWWAuthHeader: true},
		{name: "wrong password", method: http.MethodPost, setAuth: true, username: "admin", password: "wrong", wantStatus: http.StatusUnauthorized, wantWWWAuthHeader: true},
		{name: "valid credentials", method: http.MethodPost, setAuth: true, username: "admin", password: "admin", wantStatus: http.StatusOK},
		{name: "invalid method", method: http.MethodGet, setAuth: true, username: "admin", password: "admin", wantStatus: http.StatusMethodNotAllowed},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()

			req := httptest.NewRequest(tt.method, "/soap", strings.NewReader(reqBody))
			if tt.setAuth {
				req.SetBasicAuth(tt.username, tt.password)
			}

			rr := httptest.NewRecorder()
			h.HandleSOAP(rr, req)

			if rr.Code != tt.wantStatus {
				t.Fatalf("unexpected status: got=%d want=%d", rr.Code, tt.wantStatus)
			}

			if tt.wantWWWAuthHeader {
				if rr.Header().Get("WWW-Authenticate") == "" {
					t.Fatalf("expected WWW-Authenticate header to be set")
				}
			}
		})
	}
}

func TestHandleSOAP_SuccessResponses(t *testing.T) {
	t.Parallel()

	h := newTestHandler(t, stubConverter{
		getRateFn: func(context.Context, string, string) (service.ExchangeRateResult, error) {
			return service.ExchangeRateResult{
				Rate:           0.95 / 1.1,
				Source:         "ECB",
				BaseCurrency:   "USD",
				TargetCurrency: "CHF",
			}, nil
		},
		convertFn: func(context.Context, float64, string, string) (service.ConversionResult, error) {
			return service.ConversionResult{
				ConvertedAmount: 86.36363636363636,
				Rate:            0.95 / 1.1,
				Source:          "ECB",
				BaseCurrency:    "USD",
				TargetCurrency:  "CHF",
			}, nil
		},
	})

	tests := []struct {
		name      string
		fixture   string
		assertion func(t *testing.T, decoded soapEnvelopeDecoded)
	}{
		{
			name:    "GetExchangeRate success",
			fixture: "soap-get-rate-valid.xml",
			assertion: func(t *testing.T, decoded soapEnvelopeDecoded) {
				t.Helper()
				if decoded.Body.GetExchangeRateResponse == nil {
					t.Fatalf("expected GetExchangeRateResponse in SOAP body")
				}
				resp := decoded.Body.GetExchangeRateResponse
				if !almostEqual(resp.Rate, 0.95/1.1) {
					t.Fatalf("unexpected rate: got=%v", resp.Rate)
				}
				if resp.Source != "ECB" || resp.BaseCurrency != "USD" || resp.TargetCurrency != "CHF" {
					t.Fatalf("unexpected response content: %+v", *resp)
				}
			},
		},
		{
			name:    "ConvertAmount success",
			fixture: "soap-convert-valid.xml",
			assertion: func(t *testing.T, decoded soapEnvelopeDecoded) {
				t.Helper()
				if decoded.Body.ConvertAmountResponse == nil {
					t.Fatalf("expected ConvertAmountResponse in SOAP body")
				}
				resp := decoded.Body.ConvertAmountResponse
				if !almostEqual(resp.ConvertedAmount, 86.36363636363636) {
					t.Fatalf("unexpected converted amount: got=%v", resp.ConvertedAmount)
				}
				if !almostEqual(resp.Rate, 0.95/1.1) {
					t.Fatalf("unexpected rate: got=%v", resp.Rate)
				}
				if resp.Source != "ECB" || resp.BaseCurrency != "USD" || resp.TargetCurrency != "CHF" {
					t.Fatalf("unexpected response content: %+v", *resp)
				}
			},
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()
			requestBody := mustReadSOAPFixture(t, tt.fixture)

			req := httptest.NewRequest(http.MethodPost, "/soap", strings.NewReader(requestBody))
			req.SetBasicAuth("admin", "admin")
			rr := httptest.NewRecorder()

			h.HandleSOAP(rr, req)

			if rr.Code != http.StatusOK {
				t.Fatalf("unexpected status: got=%d want=%d body=%s", rr.Code, http.StatusOK, rr.Body.String())
			}
			if contentType := rr.Header().Get("Content-Type"); !strings.Contains(contentType, "text/xml") {
				t.Fatalf("expected text/xml content type, got %q", contentType)
			}

			decoded := decodeSOAPEnvelope(t, rr.Body.Bytes())
			if decoded.Body.Fault != nil {
				t.Fatalf("did not expect SOAP fault, got=%+v", *decoded.Body.Fault)
			}
			tt.assertion(t, decoded)
		})
	}
}

func TestHandleSOAP_FaultResponses(t *testing.T) {
	clientErrorHandler := newTestHandler(t, stubConverter{
		getRateFn: func(_ context.Context, from, to string) (service.ExchangeRateResult, error) {
			if strings.TrimSpace(from) == "" || strings.TrimSpace(to) == "" {
				return service.ExchangeRateResult{}, service.ErrMissingCurrency
			}
			if strings.EqualFold(strings.TrimSpace(from), "AUD") || strings.EqualFold(strings.TrimSpace(to), "AUD") {
				return service.ExchangeRateResult{}, service.UnsupportedCurrencyError{Currency: "AUD"}
			}
			return service.ExchangeRateResult{}, service.ErrMissingCurrency
		},
		convertFn: func(context.Context, float64, string, string) (service.ConversionResult, error) {
			return service.ConversionResult{}, service.ErrInvalidAmount
		},
	})

	missingFieldHandler := newTestHandler(t, stubConverter{
		getRateFn: func(context.Context, string, string) (service.ExchangeRateResult, error) {
			return service.ExchangeRateResult{}, service.ErrMissingCurrency
		},
	})

	serverErrorHandler := newTestHandler(t, stubConverter{
		getRateFn: func(context.Context, string, string) (service.ExchangeRateResult, error) {
			return service.ExchangeRateResult{}, errors.New("unexpected backend failure")
		},
	})

	tests := []struct {
		name            string
		handler         *Handler
		fixture         string
		wantStatus      int
		wantFaultCode   string
		wantFaultString string
	}{
		{
			name:            "malformed XML",
			handler:         clientErrorHandler,
			fixture:         "soap-malformed.xml",
			wantStatus:      http.StatusBadRequest,
			wantFaultCode:   clientFaultCode,
			wantFaultString: "invalid SOAP XML",
		},
		{
			name:            "missing SOAP body operation",
			handler:         clientErrorHandler,
			fixture:         "soap-missing-body.xml",
			wantStatus:      http.StatusBadRequest,
			wantFaultCode:   clientFaultCode,
			wantFaultString: "missing or unsupported SOAP operation",
		},
		{
			name:            "unknown SOAP operation",
			handler:         clientErrorHandler,
			fixture:         "soap-unknown-operation.xml",
			wantStatus:      http.StatusBadRequest,
			wantFaultCode:   clientFaultCode,
			wantFaultString: "missing or unsupported SOAP operation",
		},
		{
			name:            "missing required field",
			handler:         missingFieldHandler,
			fixture:         "soap-missing-required-field.xml",
			wantStatus:      http.StatusBadRequest,
			wantFaultCode:   clientFaultCode,
			wantFaultString: service.ErrMissingCurrency.Error(),
		},
		{
			name:            "invalid amount format",
			handler:         clientErrorHandler,
			fixture:         "soap-invalid-amount-format.xml",
			wantStatus:      http.StatusBadRequest,
			wantFaultCode:   clientFaultCode,
			wantFaultString: "invalid SOAP XML",
		},
		{
			name:            "unsupported currency",
			handler:         clientErrorHandler,
			fixture:         "soap-unsupported-currency.xml",
			wantStatus:      http.StatusBadRequest,
			wantFaultCode:   clientFaultCode,
			wantFaultString: "unsupported currency: AUD",
		},
		{
			name:            "internal service error",
			handler:         serverErrorHandler,
			fixture:         "soap-get-rate-valid.xml",
			wantStatus:      http.StatusInternalServerError,
			wantFaultCode:   serverFaultCode,
			wantFaultString: "internal service error",
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			requestBody := mustReadSOAPFixture(t, tt.fixture)
			req := httptest.NewRequest(http.MethodPost, "/soap", strings.NewReader(requestBody))
			req.SetBasicAuth("admin", "admin")
			rr := httptest.NewRecorder()

			tt.handler.HandleSOAP(rr, req)

			if rr.Code != tt.wantStatus {
				t.Fatalf("unexpected status: got=%d want=%d body=%s", rr.Code, tt.wantStatus, rr.Body.String())
			}
			if contentType := rr.Header().Get("Content-Type"); !strings.Contains(contentType, "text/xml") {
				t.Fatalf("expected text/xml content type, got %q", contentType)
			}

			decoded := decodeSOAPEnvelope(t, rr.Body.Bytes())
			if decoded.Body.Fault == nil {
				t.Fatalf("expected SOAP fault")
			}

			if decoded.Body.Fault.FaultCode != tt.wantFaultCode {
				t.Fatalf("unexpected fault code: got=%q want=%q", decoded.Body.Fault.FaultCode, tt.wantFaultCode)
			}
			if decoded.Body.Fault.FaultString != tt.wantFaultString {
				t.Fatalf("unexpected fault string: got=%q want=%q", decoded.Body.Fault.FaultString, tt.wantFaultString)
			}
			if decoded.Body.Fault.Detail.Message == "" {
				t.Fatalf("expected non-empty fault detail message")
			}
		})
	}
}

func TestHandleWSDL(t *testing.T) {
	t.Parallel()

	t.Run("GET returns WSDL", func(t *testing.T) {
		t.Parallel()

		tempDir := t.TempDir()
		wsdlPath := filepath.Join(tempDir, "currency-converter.wsdl")
		wsdlContent := `<definitions name="CurrencyConverterService"></definitions>`
		if err := os.WriteFile(wsdlPath, []byte(wsdlContent), 0o644); err != nil {
			t.Fatalf("failed to write wsdl file: %v", err)
		}

		h := &Handler{wsdlPath: wsdlPath, logger: slog.New(slog.NewTextHandler(io.Discard, nil))}
		req := httptest.NewRequest(http.MethodGet, "/wsdl", nil)
		rr := httptest.NewRecorder()

		h.HandleWSDL(rr, req)

		if rr.Code != http.StatusOK {
			t.Fatalf("unexpected status: got=%d want=%d", rr.Code, http.StatusOK)
		}
		if !strings.Contains(rr.Header().Get("Content-Type"), "text/xml") {
			t.Fatalf("unexpected content type: %q", rr.Header().Get("Content-Type"))
		}
		if rr.Body.String() != wsdlContent {
			t.Fatalf("unexpected WSDL content: got=%q want=%q", rr.Body.String(), wsdlContent)
		}
	})

	t.Run("non-GET method returns 405", func(t *testing.T) {
		t.Parallel()

		h := &Handler{logger: slog.New(slog.NewTextHandler(io.Discard, nil))}
		req := httptest.NewRequest(http.MethodPost, "/wsdl", nil)
		rr := httptest.NewRecorder()

		h.HandleWSDL(rr, req)

		if rr.Code != http.StatusMethodNotAllowed {
			t.Fatalf("unexpected status: got=%d want=%d", rr.Code, http.StatusMethodNotAllowed)
		}
	})

	t.Run("missing wsdl file returns 500", func(t *testing.T) {
		t.Parallel()

		h := &Handler{wsdlPath: filepath.Join(t.TempDir(), "missing.wsdl"), logger: slog.New(slog.NewTextHandler(io.Discard, nil))}
		req := httptest.NewRequest(http.MethodGet, "/wsdl", nil)
		rr := httptest.NewRecorder()

		h.HandleWSDL(rr, req)

		if rr.Code != http.StatusInternalServerError {
			t.Fatalf("unexpected status: got=%d want=%d", rr.Code, http.StatusInternalServerError)
		}
	})
}

func TestHandleHealth(t *testing.T) {
	t.Parallel()

	h := &Handler{}

	t.Run("GET health returns ok", func(t *testing.T) {
		t.Parallel()

		req := httptest.NewRequest(http.MethodGet, "/health", nil)
		rr := httptest.NewRecorder()
		h.HandleHealth(rr, req)

		if rr.Code != http.StatusOK {
			t.Fatalf("unexpected status: got=%d want=%d", rr.Code, http.StatusOK)
		}
		if rr.Body.String() != "ok" {
			t.Fatalf("unexpected health body: got=%q want=%q", rr.Body.String(), "ok")
		}
	})

	t.Run("non-GET returns 405", func(t *testing.T) {
		t.Parallel()

		req := httptest.NewRequest(http.MethodPost, "/health", nil)
		rr := httptest.NewRecorder()
		h.HandleHealth(rr, req)

		if rr.Code != http.StatusMethodNotAllowed {
			t.Fatalf("unexpected status: got=%d want=%d", rr.Code, http.StatusMethodNotAllowed)
		}
	})
}

type fixedRatesFetcher struct {
	data ecb.RatesData
}

func (f fixedRatesFetcher) FetchRates(context.Context) (ecb.RatesData, error) {
	return f.data, nil
}

func TestHandleSOAP_IntegrationStyle_WithRealConverter(t *testing.T) {
	t.Parallel()

	converter := service.NewConverter(fixedRatesFetcher{data: ecb.RatesData{
		Source: "ECB",
		Rates: map[string]float64{
			"EUR": 1.0,
			"USD": 1.1,
			"CHF": 0.95,
		},
	}})

	h := newTestHandler(t, converter)

	reqBody := mustReadSOAPFixture(t, "soap-convert-valid.xml")
	req := httptest.NewRequest(http.MethodPost, "/soap", strings.NewReader(reqBody))
	req.SetBasicAuth("admin", "admin")
	rr := httptest.NewRecorder()

	h.HandleSOAP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("unexpected status: got=%d want=%d body=%s", rr.Code, http.StatusOK, rr.Body.String())
	}

	decoded := decodeSOAPEnvelope(t, rr.Body.Bytes())
	if decoded.Body.ConvertAmountResponse == nil {
		t.Fatalf("expected ConvertAmountResponse in SOAP body")
	}

	resp := decoded.Body.ConvertAmountResponse
	wantRate := 0.95 / 1.1
	wantAmount := 100.0 / 1.1 * 0.95
	if !almostEqual(resp.Rate, wantRate) {
		t.Fatalf("unexpected rate: got=%v want=%v", resp.Rate, wantRate)
	}
	if !almostEqual(resp.ConvertedAmount, wantAmount) {
		t.Fatalf("unexpected converted amount: got=%v want=%v", resp.ConvertedAmount, wantAmount)
	}
	if resp.BaseCurrency != "USD" || resp.TargetCurrency != "CHF" || resp.Source != "ECB" {
		t.Fatalf("unexpected response fields: %+v", *resp)
	}
}

func newTestHandler(t *testing.T, converter converterService) *Handler {
	t.Helper()

	tempDir := t.TempDir()
	wsdlPath := filepath.Join(tempDir, "currency-converter.wsdl")
	if err := os.WriteFile(wsdlPath, []byte("<definitions/>"), 0o644); err != nil {
		t.Fatalf("failed to create temp wsdl: %v", err)
	}

	return NewHandler(converter, wsdlPath, "admin", "admin", slog.New(slog.NewTextHandler(io.Discard, nil)))
}

func decodeSOAPEnvelope(t *testing.T, data []byte) soapEnvelopeDecoded {
	t.Helper()

	var decoded soapEnvelopeDecoded
	if err := xml.Unmarshal(data, &decoded); err != nil {
		t.Fatalf("failed to decode SOAP response XML: %v\nbody=%s", err, string(data))
	}

	if decoded.XMLName.Local != "Envelope" {
		t.Fatalf("expected SOAP Envelope, got %q", decoded.XMLName.Local)
	}

	return decoded
}

func mustReadSOAPFixture(t *testing.T, name string) string {
	t.Helper()

	path := filepath.Join("testdata", name)
	b, err := os.ReadFile(path)
	if err != nil {
		t.Fatalf("failed to read fixture %s: %v", path, err)
	}

	return string(b)
}

func almostEqual(a, b float64) bool {
	const eps = 1e-9
	return math.Abs(a-b) <= eps
}
