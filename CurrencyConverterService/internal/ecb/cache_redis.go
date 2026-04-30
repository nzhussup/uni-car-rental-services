package ecb

import (
	"context"
	"errors"
	"time"

	"github.com/redis/go-redis/v9"
)

type RedisStore struct {
	client *redis.Client
}

func NewRedisStore(client *redis.Client) *RedisStore {
	return &RedisStore{client: client}
}

func (s *RedisStore) Get(ctx context.Context, key string) (string, bool, error) {
	if s == nil || s.client == nil {
		return "", false, nil
	}

	value, err := s.client.Get(ctx, key).Result()
	if err != nil {
		if errors.Is(err, redis.Nil) {
			return "", false, nil
		}
		return "", false, err
	}

	return value, true, nil
}

func (s *RedisStore) Set(ctx context.Context, key, value string, expiration time.Duration) error {
	if s == nil || s.client == nil {
		return nil
	}

	return s.client.Set(ctx, key, value, expiration).Err()
}
