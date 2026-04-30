package ecb

import (
	"context"
	"encoding/json"
	"errors"
	"testing"
	"time"
)

type stubFetcher struct {
	data  RatesData
	err   error
	count int
}

func (s *stubFetcher) FetchRates(context.Context) (RatesData, error) {
	s.count++
	if s.err != nil {
		return RatesData{}, s.err
	}
	return s.data, nil
}

type stubCache struct {
	value    string
	hasValue bool
	getErr   error
	setErr   error
	setKey   string
	setValue string
	setTTL   time.Duration
	setCalls int
	getCalls int
}

func (s *stubCache) Get(context.Context, string) (string, bool, error) {
	s.getCalls++
	return s.value, s.hasValue, s.getErr
}

func (s *stubCache) Set(_ context.Context, key, value string, expiration time.Duration) error {
	s.setCalls++
	s.setKey = key
	s.setValue = value
	s.setTTL = expiration
	return s.setErr
}

func TestCachingClient_FetchRates_UsesCachedValue(t *testing.T) {
	t.Parallel()

	cached := RatesData{
		Date:   "2026-03-02",
		Source: "cached",
		Rates: map[string]float64{
			"EUR": 1,
			"USD": 1.2,
		},
	}
	cachedBody, err := json.Marshal(cached)
	if err != nil {
		t.Fatalf("marshal cached value: %v", err)
	}

	fetcher := &stubFetcher{data: RatesData{Source: "live"}}
	store := &stubCache{value: string(cachedBody), hasValue: true}
	client := NewCachingClient(fetcher, store)

	got, err := client.FetchRates(context.Background())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if fetcher.count != 0 {
		t.Fatalf("expected no upstream fetch, got %d", fetcher.count)
	}
	if got.Source != cached.Source || got.Date != cached.Date || got.Rates["USD"] != cached.Rates["USD"] {
		t.Fatalf("unexpected cached result: %#v", got)
	}
}

func TestCachingClient_FetchRates_StoresWithTTLUntilMidnight(t *testing.T) {
	t.Parallel()

	fetcher := &stubFetcher{data: RatesData{
		Date:   "2026-03-02",
		Source: "live",
		Rates: map[string]float64{
			"EUR": 1,
			"USD": 1.1,
		},
	}}
	store := &stubCache{}
	client := NewCachingClient(fetcher, store)
	client.now = func() time.Time {
		return time.Date(2026, 3, 2, 10, 15, 0, 0, time.UTC)
	}

	got, err := client.FetchRates(context.Background())
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if fetcher.count != 1 {
		t.Fatalf("expected one upstream fetch, got %d", fetcher.count)
	}
	if store.setCalls != 1 {
		t.Fatalf("expected one cache write, got %d", store.setCalls)
	}
	if store.setKey != "ecb:daily-rates" {
		t.Fatalf("unexpected cache key: %q", store.setKey)
	}
	wantTTL := 13*time.Hour + 45*time.Minute
	if store.setTTL != wantTTL {
		t.Fatalf("unexpected ttl: got=%v want=%v", store.setTTL, wantTTL)
	}
	if got.Source != fetcher.data.Source || got.Rates["USD"] != fetcher.data.Rates["USD"] {
		t.Fatalf("unexpected fetched result: %#v", got)
	}
}

func TestCachingClient_FetchRates_GracefullyFallsBackOnCacheError(t *testing.T) {
	t.Parallel()

	fetcher := &stubFetcher{data: RatesData{Source: "live", Rates: map[string]float64{"EUR": 1, "USD": 1.1}}}
	store := &stubCache{getErr: errors.New("redis down")}
	client := NewCachingClient(fetcher, store)

	if _, err := client.FetchRates(context.Background()); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if fetcher.count != 1 {
		t.Fatalf("expected upstream fetch after cache failure, got %d", fetcher.count)
	}
}
