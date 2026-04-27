package server

import (
	"context"
	"errors"
	"log/slog"
	"net"
	"os"
	"path/filepath"
	"strings"
	"testing"

	apperr "currency-converter-service/internal/err"
	"currency-converter-service/internal/service"
	currencyconverterpb "currency-converter-service/proto"

	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/status"
	"google.golang.org/grpc/test/bufconn"
	"google.golang.org/protobuf/encoding/protojson"
	"google.golang.org/protobuf/proto"
)

const bufSize = 1024 * 1024

type converterMock struct {
	getExchangeRateFn        func(ctx context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error)
	convertAmountFn          func(ctx context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error)
	getSupportedCurrenciesFn func(ctx context.Context) ([]string, error)
}

func (m converterMock) GetExchangeRate(ctx context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error) {
	if m.getExchangeRateFn == nil {
		return service.ExchangeRateResult{}, nil
	}
	return m.getExchangeRateFn(ctx, fromCurrency, toCurrency)
}

func (m converterMock) ConvertAmount(ctx context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error) {
	if m.convertAmountFn == nil {
		return service.ConversionResult{}, nil
	}
	return m.convertAmountFn(ctx, amount, fromCurrency, toCurrency)
}

func (m converterMock) GetSupportedCurrencies(ctx context.Context) ([]string, error) {
	if m.getSupportedCurrenciesFn == nil {
		return nil, nil
	}
	return m.getSupportedCurrenciesFn(ctx)
}

func TestServerGRPC_GetExchangeRate(t *testing.T) {
	t.Parallel()

	req := &currencyconverterpb.GetExchangeRateRequest{}
	mustLoadProtoJSON(t, "grpc-get-rate-valid.json", req)
	client, cleanup := newBufconnClient(t, converterMock{
		getExchangeRateFn: func(_ context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error) {
			if fromCurrency != "USD" || toCurrency != "CHF" {
				t.Fatalf("unexpected request values: from=%q to=%q", fromCurrency, toCurrency)
			}
			return service.ExchangeRateResult{
				Rate:           0.89,
				Source:         "ECB",
				BaseCurrency:   "USD",
				TargetCurrency: "CHF",
			}, nil
		},
	})
	defer cleanup()

	resp, err := client.GetExchangeRate(context.Background(), req)
	if err != nil {
		t.Fatalf("unexpected RPC error: %v", err)
	}
	if resp.GetRate() != 0.89 {
		t.Fatalf("unexpected rate: got=%v want=%v", resp.GetRate(), 0.89)
	}
}

func TestServerGRPC_ConvertAmount(t *testing.T) {
	t.Parallel()

	req := &currencyconverterpb.ConvertAmountRequest{}
	mustLoadProtoJSON(t, "grpc-convert-valid.json", req)
	client, cleanup := newBufconnClient(t, converterMock{
		convertAmountFn: func(_ context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error) {
			if amount != 100 || fromCurrency != "USD" || toCurrency != "CHF" {
				t.Fatalf("unexpected request values: amount=%v from=%q to=%q", amount, fromCurrency, toCurrency)
			}
			return service.ConversionResult{
				ConvertedAmount: 89.0,
				Rate:            0.89,
				Source:          "ECB",
				BaseCurrency:    "USD",
				TargetCurrency:  "CHF",
			}, nil
		},
	})
	defer cleanup()

	resp, err := client.ConvertAmount(context.Background(), req)
	if err != nil {
		t.Fatalf("unexpected RPC error: %v", err)
	}
	if resp.GetConvertedAmount() != 89 {
		t.Fatalf("unexpected converted amount: got=%v want=%v", resp.GetConvertedAmount(), 89.0)
	}
}

func TestServerGRPC_GetSupportedCurrencies(t *testing.T) {
	t.Parallel()

	req := &currencyconverterpb.GetSupportedCurrenciesRequest{}
	mustLoadProtoJSON(t, "grpc-get-supported-currencies.json", req)
	client, cleanup := newBufconnClient(t, converterMock{
		getSupportedCurrenciesFn: func(_ context.Context) ([]string, error) {
			return []string{"CHF", "EUR", "USD"}, nil
		},
	})
	defer cleanup()

	resp, err := client.GetSupportedCurrencies(context.Background(), req)
	if err != nil {
		t.Fatalf("unexpected RPC error: %v", err)
	}

	want := []string{"CHF", "EUR", "USD"}
	if strings.Join(resp.GetCurrencies(), ",") != strings.Join(want, ",") {
		t.Fatalf("unexpected currencies: got=%v want=%v", resp.GetCurrencies(), want)
	}
}

func TestServerGRPC_ErrorMapping(t *testing.T) {
	t.Parallel()

	tests := []struct {
		name         string
		fixture      string
		serviceError error
		wantCode     codes.Code
	}{
		{
			name:         "missing currency maps to invalid argument",
			fixture:      "grpc-get-rate-missing-currency.json",
			serviceError: apperr.ErrMissingCurrency,
			wantCode:     codes.InvalidArgument,
		},
		{
			name:         "unsupported currency maps to invalid argument",
			fixture:      "grpc-get-rate-unsupported-currency.json",
			serviceError: apperr.UnsupportedCurrencyError{Currency: "AUD"},
			wantCode:     codes.InvalidArgument,
		},
		{
			name:         "fetch errors map to failed precondition",
			fixture:      "grpc-convert-invalid-amount.json",
			serviceError: errors.Join(apperr.ErrFetch, errors.New("upstream unavailable")),
			wantCode:     codes.FailedPrecondition,
		},
	}

	for _, tt := range tests {
		tt := tt
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()

			client, cleanup := newBufconnClient(t, converterMock{
				getExchangeRateFn: func(_ context.Context, _, _ string) (service.ExchangeRateResult, error) {
					return service.ExchangeRateResult{}, tt.serviceError
				},
				convertAmountFn: func(_ context.Context, _ float64, _, _ string) (service.ConversionResult, error) {
					return service.ConversionResult{}, tt.serviceError
				},
			})
			defer cleanup()

			var err error
			if strings.Contains(tt.fixture, "convert") {
				req := &currencyconverterpb.ConvertAmountRequest{}
				mustLoadProtoJSON(t, tt.fixture, req)
				_, err = client.ConvertAmount(context.Background(), req)
			} else {
				req := &currencyconverterpb.GetExchangeRateRequest{}
				mustLoadProtoJSON(t, tt.fixture, req)
				_, err = client.GetExchangeRate(context.Background(), req)
			}

			if err == nil {
				t.Fatalf("expected RPC error")
			}
			if got := status.Code(err); got != tt.wantCode {
				t.Fatalf("unexpected gRPC code: got=%s want=%s (err=%v)", got, tt.wantCode, err)
			}
		})
	}
}

func newBufconnClient(t *testing.T, converter converterService) (currencyconverterpb.CurrencyConverterClient, func()) {
	t.Helper()

	listener := bufconn.Listen(bufSize)
	logger := slog.New(slog.NewTextHandler(os.Stdout, nil))
	srv := NewServer(converter, logger, 0, nil)

	go func() {
		_ = srv.grpcServer.Serve(listener)
	}()

	conn, err := grpc.NewClient(
		"passthrough:///bufnet",
		grpc.WithContextDialer(func(context.Context, string) (net.Conn, error) {
			return listener.Dial()
		}),
		grpc.WithTransportCredentials(insecure.NewCredentials()),
	)
	if err != nil {
		t.Fatalf("failed to dial bufconn server: %v", err)
	}

	cleanup := func() {
		_ = conn.Close()
		srv.grpcServer.Stop()
		_ = listener.Close()
	}

	return currencyconverterpb.NewCurrencyConverterClient(conn), cleanup
}

func mustLoadProtoJSON(t *testing.T, fixture string, msg proto.Message) {
	t.Helper()

	path := filepath.Join("testdata", fixture)
	b, err := os.ReadFile(path)
	if err != nil {
		t.Fatalf("failed to read fixture %s: %v", path, err)
	}
	if err := protojson.Unmarshal(b, msg); err != nil {
		t.Fatalf("failed to unmarshal fixture %s: %v", path, err)
	}
}
