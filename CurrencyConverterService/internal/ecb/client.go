package ecb

import (
	"context"
	"encoding/json"
	"encoding/xml"
	"fmt"
	"io"
	"net/http"
	"strconv"
	"strings"
	"time"

	apperr "currency-converter-service/internal/err"
)

type Client struct {
	feedURL    string
	httpClient *http.Client
}

type ratesFetcher interface {
	FetchRates(ctx context.Context) (RatesData, error)
}

type cacheStore interface {
	Get(ctx context.Context, key string) (string, bool, error)
	Set(ctx context.Context, key, value string, expiration time.Duration) error
}

type CachingClient struct {
	fetcher  ratesFetcher
	store    cacheStore
	now      func() time.Time
	cacheKey string
}

func NewClient(feedURL string, httpClient *http.Client) *Client {
	if httpClient == nil {
		httpClient = &http.Client{Timeout: 10 * time.Second}
	}

	return &Client{
		feedURL:    feedURL,
		httpClient: httpClient,
	}
}

func NewCachingClient(fetcher ratesFetcher, store cacheStore) *CachingClient {
	return &CachingClient{
		fetcher:  fetcher,
		store:    store,
		now:      time.Now,
		cacheKey: "ecb:daily-rates",
	}
}

func (c *Client) FetchRates(ctx context.Context) (RatesData, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, c.feedURL, nil)
	if err != nil {
		return RatesData{}, fmt.Errorf("create ECB request: %w", err)
	}

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return RatesData{}, fmt.Errorf("fetch ECB feed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return RatesData{}, fmt.Errorf("ECB feed returned status %d", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return RatesData{}, fmt.Errorf("read ECB feed body: %w", err)
	}

	return ParseRatesXML(body, c.feedURL)
}

func (c *CachingClient) FetchRates(ctx context.Context) (RatesData, error) {
	if c == nil || c.fetcher == nil {
		return RatesData{}, fmt.Errorf("missing ECB fetcher")
	}

	if c.store != nil {
		if cached, ok, err := c.store.Get(ctx, c.cacheKey); err == nil && ok {
			var cachedRates RatesData
			if err := json.Unmarshal([]byte(cached), &cachedRates); err == nil {
				return cachedRates, nil
			}
		}
	}

	rates, err := c.fetcher.FetchRates(ctx)
	if err != nil {
		return RatesData{}, err
	}

	if c.store != nil {
		if cached, err := json.Marshal(rates); err == nil {
			_ = c.store.Set(ctx, c.cacheKey, string(cached), ttlUntilNextMidnight(c.now()))
		}
	}

	return rates, nil
}

func ttlUntilNextMidnight(now time.Time) time.Duration {
	utc := now.UTC()
	nextMidnight := time.Date(utc.Year(), utc.Month(), utc.Day()+1, 0, 0, 0, 0, time.UTC)
	return nextMidnight.Sub(utc)
}

func ParseRatesXML(data []byte, source string) (RatesData, error) {
	var doc envelope
	if err := xml.Unmarshal(data, &doc); err != nil {
		return RatesData{}, fmt.Errorf("parse ECB XML: %w", err)
	}

	if doc.Cube.Daily.Time == "" {
		return RatesData{}, apperr.ErrECBMissingDailyTimestamp
	}

	rates := map[string]float64{"EUR": 1.0}
	for _, r := range doc.Cube.Daily.Rates {
		currency := strings.ToUpper(strings.TrimSpace(r.Currency))
		if currency == "" {
			return RatesData{}, apperr.ErrECBEmptyCurrencyCode
		}

		rate, err := strconv.ParseFloat(strings.TrimSpace(r.Rate), 64)
		if err != nil {
			return RatesData{}, fmt.Errorf("invalid ECB rate for %s: %w", currency, err)
		}
		if rate <= 0 {
			return RatesData{}, fmt.Errorf("non-positive ECB rate for %s", currency)
		}

		rates[currency] = rate
	}

	if len(rates) == 1 {
		return RatesData{}, apperr.ErrECBNoExchangeRates
	}

	return RatesData{
		Date:   doc.Cube.Daily.Time,
		Source: source,
		Rates:  rates,
	}, nil
}
