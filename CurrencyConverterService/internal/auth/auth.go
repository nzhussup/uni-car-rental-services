package auth

import (
	"context"
	apperr "currency-converter-service/internal/err"
	"encoding/base64"
	"errors"
	"log/slog"
	"strings"

	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/metadata"
	"google.golang.org/grpc/status"
)

func BasicAuthUnaryInterceptor(expectedUsername, expectedPassword string, logger *slog.Logger) grpc.UnaryServerInterceptor {
	return func(
		ctx context.Context,
		req interface{},
		info *grpc.UnaryServerInfo,
		handler grpc.UnaryHandler,
	) (interface{}, error) {
		md, ok := metadata.FromIncomingContext(ctx)
		if !ok {
			return nil, status.Error(codes.Unauthenticated, apperr.ErrAuthMissingMetadata.Error())
		}

		authValues := md.Get("authorization")
		if len(authValues) == 0 {
			return nil, status.Error(codes.Unauthenticated, apperr.ErrAuthMissingAuthorization.Error())
		}

		username, password, err := parseBasicAuthorization(authValues[0])
		if err != nil || username != expectedUsername || password != expectedPassword {
			if logger != nil {
				logger.Warn("grpc authentication failed", "method", info.FullMethod)
			}
			return nil, status.Error(codes.Unauthenticated, apperr.ErrAuthInvalidCredentials.Error())
		}

		return handler(ctx, req)
	}
}

func parseBasicAuthorization(value string) (string, string, error) {
	const prefix = "Basic "
	if !strings.HasPrefix(value, prefix) {
		return "", "", apperr.ErrAuthParseNotBasic
	}

	decoded, err := base64.StdEncoding.DecodeString(strings.TrimSpace(strings.TrimPrefix(value, prefix)))
	if err != nil {
		return "", "", errors.Join(apperr.ErrAuthParseInvalidBase64, err)
	}

	parts := strings.SplitN(string(decoded), ":", 2)
	if len(parts) != 2 {
		return "", "", apperr.ErrAuthParseInvalidFormat
	}

	return parts[0], parts[1], nil
}
