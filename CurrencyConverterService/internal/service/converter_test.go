package service

import (
	"context"
	"errors"
	"math"
	"sort"
	"strings"
	"testing"

	"currency-converter-service/internal/ecb"
	apperr "currency-converter-service/internal/err"
)

type staticFetcher struct {
	data ecb.RatesData
	err  error
}

func (s staticFetcher) FetchRates(context.Context) (ecb.RatesData, error) {
	if s.err != nil {
		return ecb.RatesData{}, s.err
	}
	return s.data, nil
}

func sampleRatesData() ecb.RatesData {
	return ecb.RatesData{
		Source: "ECB",
		Rates: map[string]float64{
			"EUR": 1.0,
			"USD": 1.1,
			"GBP": 0.85,
			"JPY": 160.0,
			"CHF": 0.95,
		},
	}
}

func TestConverter_GetExchangeRate(t *testing.T) {
	t.Parallel()

	tests := []struct {
		name        string
		fetcher     staticFetcher
		from        string
		to          string
		wantRate    float64
		wantBase    string
		wantTarget  string
		errContains string
	}{
		{
			name:       "EUR to USD direct rate",
			fetcher:    staticFetcher{data: sampleRatesData()},
			from:       "EUR",
			to:         "USD",
			wantRate:   1.1,
			wantBase:   "EUR",
			wantTarget: "USD",
		},
		{
			name:       "USD to EUR inverse rate",
			fetcher:    staticFetcher{data: sampleRatesData()},
			from:       "USD",
			to:         "EUR",
			wantRate:   1.0 / 1.1,
			wantBase:   "USD",
			wantTarget: "EUR",
		},
		{
			name:       "JPY to GBP cross rate",
			fetcher:    staticFetcher{data: sampleRatesData()},
			from:       "JPY",
			to:         "GBP",
			wantRate:   0.85 / 160.0,
			wantBase:   "JPY",
			wantTarget: "GBP",
		},
		{
			name:       "same currency EUR to EUR",
			fetcher:    staticFetcher{data: sampleRatesData()},
			from:       "EUR",
			to:         "EUR",
			wantRate:   1.0,
			wantBase:   "EUR",
			wantTarget: "EUR",
		},
		{
			name:       "same currency USD to USD",
			fetcher:    staticFetcher{data: sampleRatesData()},
			from:       "USD",
			to:         "USD",
			wantRate:   1.0,
			wantBase:   "USD",
			wantTarget: "USD",
		},
		{
			name:       "lowercase currencies are normalized",
			fetcher:    staticFetcher{data: sampleRatesData()},
			from:       "usd",
			to:         "chf",
			wantRate:   0.95 / 1.1,
			wantBase:   "USD",
			wantTarget: "CHF",
		},
		{
			name:        "unsupported from currency",
			fetcher:     staticFetcher{data: sampleRatesData()},
			from:        "AUD",
			to:          "EUR",
			errContains: "unsupported currency: AUD",
		},
		{
			name:        "unsupported to currency",
			fetcher:     staticFetcher{data: sampleRatesData()},
			from:        "EUR",
			to:          "CAD",
			errContains: "unsupported currency: CAD",
		},
		{
			name:        "missing currency value",
			fetcher:     staticFetcher{data: sampleRatesData()},
			from:        "EUR",
			to:          "",
			errContains: apperr.ErrMissingCurrency.Error(),
		},
		{
			name:        "fetcher error propagates",
			fetcher:     staticFetcher{err: errors.New("upstream failed")},
			from:        "EUR",
			to:          "USD",
			errContains: apperr.ErrFetch.Error(),
		},
		{
			name: "missing required rate in map",
			fetcher: staticFetcher{data: ecb.RatesData{Source: "ECB", Rates: map[string]float64{
				"USD": 1.1,
			}}},
			from:        "EUR",
			to:          "USD",
			errContains: "unsupported currency: EUR",
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()

			converter := NewConverter(tt.fetcher)
			got, err := converter.GetExchangeRate(context.Background(), tt.from, tt.to)

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

			if !almostEqual(got.Rate, tt.wantRate) {
				t.Fatalf("unexpected rate: got=%v want=%v", got.Rate, tt.wantRate)
			}
			if got.BaseCurrency != tt.wantBase {
				t.Fatalf("unexpected base currency: got=%q want=%q", got.BaseCurrency, tt.wantBase)
			}
			if got.TargetCurrency != tt.wantTarget {
				t.Fatalf("unexpected target currency: got=%q want=%q", got.TargetCurrency, tt.wantTarget)
			}
			if got.Source != "ECB" {
				t.Fatalf("unexpected source: got=%q want=%q", got.Source, "ECB")
			}
		})
	}
}

func TestConverter_ConvertAmount(t *testing.T) {
	t.Parallel()

	tests := []struct {
		name             string
		fetcher          staticFetcher
		amount           float64
		from             string
		to               string
		wantAmount       float64
		wantRate         float64
		wantBase         string
		wantTarget       string
		errContains      string
		wantExactNoDelta bool
	}{
		{
			name:       "EUR to USD valid conversion",
			fetcher:    staticFetcher{data: sampleRatesData()},
			amount:     100,
			from:       "EUR",
			to:         "USD",
			wantAmount: 110,
			wantRate:   1.1,
			wantBase:   "EUR",
			wantTarget: "USD",
		},
		{
			name:       "USD to EUR valid conversion",
			fetcher:    staticFetcher{data: sampleRatesData()},
			amount:     110,
			from:       "USD",
			to:         "EUR",
			wantAmount: 100,
			wantRate:   1.0 / 1.1,
			wantBase:   "USD",
			wantTarget: "EUR",
		},
		{
			name:       "cross-rate conversion JPY to GBP",
			fetcher:    staticFetcher{data: sampleRatesData()},
			amount:     1600,
			from:       "JPY",
			to:         "GBP",
			wantAmount: 8.5,
			wantRate:   0.85 / 160.0,
			wantBase:   "JPY",
			wantTarget: "GBP",
		},
		{
			name:       "fractional amount conversion",
			fetcher:    staticFetcher{data: sampleRatesData()},
			amount:     12.34,
			from:       "USD",
			to:         "CHF",
			wantAmount: 12.34 / 1.1 * 0.95,
			wantRate:   0.95 / 1.1,
			wantBase:   "USD",
			wantTarget: "CHF",
		},
		{
			name:       "very large amount conversion",
			fetcher:    staticFetcher{data: sampleRatesData()},
			amount:     1_000_000_000,
			from:       "EUR",
			to:         "JPY",
			wantAmount: 160_000_000_000,
			wantRate:   160,
			wantBase:   "EUR",
			wantTarget: "JPY",
		},
		{
			name:             "same currency keeps amount",
			fetcher:          staticFetcher{data: sampleRatesData()},
			amount:           77.77,
			from:             "USD",
			to:               "USD",
			wantAmount:       77.77,
			wantRate:         1.0,
			wantBase:         "USD",
			wantTarget:       "USD",
			wantExactNoDelta: true,
		},
		{
			name:       "lowercase currencies are normalized",
			fetcher:    staticFetcher{data: sampleRatesData()},
			amount:     100,
			from:       "usd",
			to:         "chf",
			wantAmount: 100 / 1.1 * 0.95,
			wantRate:   0.95 / 1.1,
			wantBase:   "USD",
			wantTarget: "CHF",
		},
		{
			name:        "amount equals zero",
			fetcher:     staticFetcher{data: sampleRatesData()},
			amount:      0,
			from:        "EUR",
			to:          "USD",
			errContains: apperr.ErrInvalidAmount.Error(),
		},
		{
			name:        "negative amount",
			fetcher:     staticFetcher{data: sampleRatesData()},
			amount:      -1,
			from:        "EUR",
			to:          "USD",
			errContains: apperr.ErrInvalidAmount.Error(),
		},
		{
			name:        "unsupported currency",
			fetcher:     staticFetcher{data: sampleRatesData()},
			amount:      10,
			from:        "EUR",
			to:          "AUD",
			errContains: "unsupported currency: AUD",
		},
		{
			name:        "missing currency",
			fetcher:     staticFetcher{data: sampleRatesData()},
			amount:      10,
			from:        "",
			to:          "USD",
			errContains: apperr.ErrMissingCurrency.Error(),
		},
		{
			name:        "fetcher error propagates",
			fetcher:     staticFetcher{err: errors.New("backend down")},
			amount:      10,
			from:        "EUR",
			to:          "USD",
			errContains: apperr.ErrFetch.Error(),
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()

			converter := NewConverter(tt.fetcher)
			got, err := converter.ConvertAmount(context.Background(), tt.amount, tt.from, tt.to)

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

			if tt.wantExactNoDelta {
				if got.ConvertedAmount != tt.wantAmount {
					t.Fatalf("unexpected converted amount: got=%v want=%v", got.ConvertedAmount, tt.wantAmount)
				}
			} else if !almostEqual(got.ConvertedAmount, tt.wantAmount) {
				t.Fatalf("unexpected converted amount: got=%v want=%v", got.ConvertedAmount, tt.wantAmount)
			}

			if !almostEqual(got.Rate, tt.wantRate) {
				t.Fatalf("unexpected rate: got=%v want=%v", got.Rate, tt.wantRate)
			}
			if got.BaseCurrency != tt.wantBase {
				t.Fatalf("unexpected base currency: got=%q want=%q", got.BaseCurrency, tt.wantBase)
			}
			if got.TargetCurrency != tt.wantTarget {
				t.Fatalf("unexpected target currency: got=%q want=%q", got.TargetCurrency, tt.wantTarget)
			}
			if got.Source != "ECB" {
				t.Fatalf("unexpected source: got=%q want=%q", got.Source, "ECB")
			}
		})
	}
}

func TestConverter_GetSupportedCurrencies(t *testing.T) {
	t.Parallel()

	converter := NewConverter(staticFetcher{data: sampleRatesData()})
	got, err := converter.GetSupportedCurrencies(context.Background())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}

	if len(got) != 5 {
		t.Fatalf("unexpected currency count: got=%d want=%d", len(got), 5)
	}

	want := append([]string(nil), got...)
	sort.Strings(want)
	if strings.Join(got, ",") != strings.Join(want, ",") {
		t.Fatalf("currencies should be sorted: got=%v", got)
	}
}

func almostEqual(a, b float64) bool {
	const eps = 1e-9
	return math.Abs(a-b) <= eps
}
