package ecb

import (
	"context"
	"net/http"
	"net/http/httptest"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func TestParseRatesXML(t *testing.T) {
	t.Parallel()

	tests := []struct {
		name        string
		xmlFixture  string
		source      string
		wantDate    string
		wantRates   map[string]float64
		errContains string
	}{
		{
			name:       "valid ECB XML parses and normalizes currencies",
			xmlFixture: "ecb-valid.xml",
			source:     "http://example.test/ecb.xml",
			wantDate:   "2026-03-02",
			wantRates: map[string]float64{
				"EUR": 1.0,
				"USD": 1.1,
				"GBP": 0.85,
				"JPY": 160.0,
				"CHF": 0.95,
			},
		},
		{
			name:        "malformed XML returns parse error",
			xmlFixture:  "ecb-malformed.xml",
			source:      "src",
			errContains: "parse ECB XML",
		},
		{
			name:        "missing daily timestamp returns error",
			xmlFixture:  "ecb-missing-time.xml",
			source:      "src",
			errContains: "missing daily rate timestamp",
		},
		{
			name:        "invalid rate format returns error",
			xmlFixture:  "ecb-invalid-rate.xml",
			source:      "src",
			errContains: "invalid ECB rate for USD",
		},
		{
			name:        "non-positive rate returns error",
			xmlFixture:  "ecb-non-positive-rate.xml",
			source:      "src",
			errContains: "non-positive ECB rate for USD",
		},
		{
			name:        "empty currency returns error",
			xmlFixture:  "ecb-empty-currency.xml",
			source:      "src",
			errContains: "empty currency code",
		},
		{
			name:        "no rates returns error",
			xmlFixture:  "ecb-no-rates.xml",
			source:      "src",
			errContains: "contains no exchange rates",
		},
		{
			name:       "duplicate currencies keep last rate",
			xmlFixture: "ecb-duplicate-currency.xml",
			source:     "src",
			wantDate:   "2026-03-02",
			wantRates: map[string]float64{
				"EUR": 1.0,
				"USD": 1.2,
			},
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()
			xmlBody := mustReadTestFile(t, tt.xmlFixture)

			got, err := ParseRatesXML([]byte(xmlBody), tt.source)
			if tt.errContains != "" {
				if err == nil {
					t.Fatalf("expected error containing %q, got nil", tt.errContains)
				}
				if !strings.Contains(err.Error(), tt.errContains) {
					t.Fatalf("expected error containing %q, got %q", tt.errContains, err.Error())
				}
				return
			}

			if err != nil {
				t.Fatalf("unexpected error: %v", err)
			}

			if got.Date != tt.wantDate {
				t.Fatalf("unexpected date: got=%q want=%q", got.Date, tt.wantDate)
			}
			if got.Source != tt.source {
				t.Fatalf("unexpected source: got=%q want=%q", got.Source, tt.source)
			}
			for currency, wantRate := range tt.wantRates {
				gotRate, ok := got.Rates[currency]
				if !ok {
					t.Fatalf("missing expected currency %q", currency)
				}
				if gotRate != wantRate {
					t.Fatalf("unexpected rate for %s: got=%v want=%v", currency, gotRate, wantRate)
				}
			}
		})
	}
}

func TestClient_FetchRates(t *testing.T) {
	t.Parallel()

	tests := []struct {
		name           string
		statusCode     int
		bodyFixture    string
		sleep          time.Duration
		clientTimeout  time.Duration
		wantErrContain string
		assertSuccess  bool
	}{
		{
			name:          "success",
			statusCode:    http.StatusOK,
			bodyFixture:   "ecb-valid.xml",
			clientTimeout: 2 * time.Second,
			assertSuccess: true,
		},
		{
			name:           "non-200 response",
			statusCode:     http.StatusBadGateway,
			bodyFixture:    "ecb-non-200-body.txt",
			clientTimeout:  2 * time.Second,
			wantErrContain: "ECB feed returned status 502",
		},
		{
			name:           "invalid XML body",
			statusCode:     http.StatusOK,
			bodyFixture:    "ecb-malformed.xml",
			clientTimeout:  2 * time.Second,
			wantErrContain: "parse ECB XML",
		},
		{
			name:           "empty body",
			statusCode:     http.StatusOK,
			bodyFixture:    "ecb-empty.xml",
			clientTimeout:  2 * time.Second,
			wantErrContain: "parse ECB XML",
		},
		{
			name:           "timeout",
			statusCode:     http.StatusOK,
			bodyFixture:    "ecb-valid.xml",
			sleep:          150 * time.Millisecond,
			clientTimeout:  20 * time.Millisecond,
			wantErrContain: "fetch ECB feed",
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()
			responseBody := mustReadTestFile(t, tt.bodyFixture)

			srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
				if tt.sleep > 0 {
					time.Sleep(tt.sleep)
				}
				w.WriteHeader(tt.statusCode)
				_, _ = w.Write([]byte(responseBody))
			}))
			defer srv.Close()

			client := NewClient(srv.URL, &http.Client{Timeout: tt.clientTimeout})
			got, err := client.FetchRates(context.Background())

			if tt.wantErrContain != "" {
				if err == nil {
					t.Fatalf("expected error containing %q, got nil", tt.wantErrContain)
				}
				if !strings.Contains(err.Error(), tt.wantErrContain) {
					t.Fatalf("expected error containing %q, got %q", tt.wantErrContain, err.Error())
				}
				return
			}

			if err != nil {
				t.Fatalf("unexpected error: %v", err)
			}
			if !tt.assertSuccess {
				t.Fatalf("test configuration error: success case must set assertSuccess")
			}

			if got.Source != srv.URL {
				t.Fatalf("unexpected source: got=%q want=%q", got.Source, srv.URL)
			}
			if got.Rates["EUR"] != 1.0 {
				t.Fatalf("expected EUR=1.0, got %v", got.Rates["EUR"])
			}
			if got.Rates["USD"] != 1.1 {
				t.Fatalf("expected USD=1.1, got %v", got.Rates["USD"])
			}
		})
	}
}

func mustReadTestFile(t *testing.T, name string) string {
	t.Helper()

	path := filepath.Join("testdata", name)
	b, err := os.ReadFile(path)
	if err != nil {
		t.Fatalf("failed to read %s: %v", path, err)
	}
	return string(b)
}
