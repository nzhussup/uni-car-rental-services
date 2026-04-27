package service

import (
	"context"
	"errors"
	"sort"
	"strings"

	"currency-converter-service/internal/ecb"
	apperr "currency-converter-service/internal/err"
)

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
		return ExchangeRateResult{}, errors.Join(apperr.ErrFetch, err)
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
		return ConversionResult{}, apperr.ErrInvalidAmount
	}

	from, to, err := normalizePair(fromCurrency, toCurrency)
	if err != nil {
		return ConversionResult{}, err
	}

	ratesData, err := c.fetcher.FetchRates(ctx)
	if err != nil {
		return ConversionResult{}, errors.Join(apperr.ErrFetch, err)
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
		return nil, errors.Join(apperr.ErrFetch, err)
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
		return "", "", apperr.ErrMissingCurrency
	}

	return from, to, nil
}

func calculateRate(rates map[string]float64, from, to string) (float64, error) {
	fromRate, ok := rates[from]
	if !ok {
		return 0, apperr.UnsupportedCurrencyError{Currency: from}
	}

	toRate, ok := rates[to]
	if !ok {
		return 0, apperr.UnsupportedCurrencyError{Currency: to}
	}

	return toRate / fromRate, nil
}
