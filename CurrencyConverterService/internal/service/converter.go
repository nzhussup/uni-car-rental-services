package service

import (
	"context"
	"errors"
	"fmt"
	"sort"
	"strings"

	"currency-converter-service/internal/ecb"
)

var (
	ErrMissingCurrency = errors.New("currency code is required")
	ErrInvalidAmount   = errors.New("amount must be greater than zero")
)

type UnsupportedCurrencyError struct {
	Currency string
}

func (e UnsupportedCurrencyError) Error() string {
	return fmt.Sprintf("unsupported currency: %s", e.Currency)
}

func IsClientError(err error) bool {
	if err == nil {
		return false
	}

	if errors.Is(err, ErrMissingCurrency) || errors.Is(err, ErrInvalidAmount) {
		return true
	}

	var unsupported UnsupportedCurrencyError
	return errors.As(err, &unsupported)
}

type RatesFetcher interface {
	FetchRates(ctx context.Context) (ecb.RatesData, error)
}

type Converter struct {
	fetcher RatesFetcher
}

type ExchangeRateResult struct {
	Rate           float64
	Source         string
	BaseCurrency   string
	TargetCurrency string
}

type ConversionResult struct {
	ConvertedAmount float64
	Rate            float64
	Source          string
	BaseCurrency    string
	TargetCurrency  string
}

func NewConverter(fetcher RatesFetcher) *Converter {
	return &Converter{fetcher: fetcher}
}

func (c *Converter) GetExchangeRate(ctx context.Context, fromCurrency, toCurrency string) (ExchangeRateResult, error) {
	from, to, err := normalizePair(fromCurrency, toCurrency)
	if err != nil {
		return ExchangeRateResult{}, err
	}

	ratesData, err := c.fetcher.FetchRates(ctx)
	if err != nil {
		return ExchangeRateResult{}, fmt.Errorf("fetch rates: %w", err)
	}

	rate, err := calculateRate(ratesData.Rates, from, to)
	if err != nil {
		return ExchangeRateResult{}, err
	}

	return ExchangeRateResult{
		Rate:           rate,
		Source:         ratesData.Source,
		BaseCurrency:   from,
		TargetCurrency: to,
	}, nil
}

func (c *Converter) ConvertAmount(ctx context.Context, amount float64, fromCurrency, toCurrency string) (ConversionResult, error) {
	if amount <= 0 {
		return ConversionResult{}, ErrInvalidAmount
	}

	from, to, err := normalizePair(fromCurrency, toCurrency)
	if err != nil {
		return ConversionResult{}, err
	}

	ratesData, err := c.fetcher.FetchRates(ctx)
	if err != nil {
		return ConversionResult{}, fmt.Errorf("fetch rates: %w", err)
	}

	rate, err := calculateRate(ratesData.Rates, from, to)
	if err != nil {
		return ConversionResult{}, err
	}

	convertedAmount := amount / ratesData.Rates[from] * ratesData.Rates[to]

	return ConversionResult{
		ConvertedAmount: convertedAmount,
		Rate:            rate,
		Source:          ratesData.Source,
		BaseCurrency:    from,
		TargetCurrency:  to,
	}, nil
}

func (c *Converter) GetSupportedCurrencies(ctx context.Context) ([]string, error) {
	ratesData, err := c.fetcher.FetchRates(ctx)
	if err != nil {
		return nil, fmt.Errorf("fetch rates: %w", err)
	}

	currencies := make([]string, 0, len(ratesData.Rates))
	for currency := range ratesData.Rates {
		currencies = append(currencies, currency)
	}

	sort.Strings(currencies)
	return currencies, nil
}

func normalizePair(fromCurrency, toCurrency string) (string, string, error) {
	from := strings.ToUpper(strings.TrimSpace(fromCurrency))
	to := strings.ToUpper(strings.TrimSpace(toCurrency))

	if from == "" || to == "" {
		return "", "", ErrMissingCurrency
	}

	return from, to, nil
}

func calculateRate(rates map[string]float64, from, to string) (float64, error) {
	fromRate, ok := rates[from]
	if !ok {
		return 0, UnsupportedCurrencyError{Currency: from}
	}

	toRate, ok := rates[to]
	if !ok {
		return 0, UnsupportedCurrencyError{Currency: to}
	}

	return toRate / fromRate, nil
}
