package auth

import (
	"context"
	apperr "currency-converter-service/internal/err"
	"encoding/base64"
	"log/slog"
	"os"
	"testing"

	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/metadata"
	"google.golang.org/grpc/status"
)

func TestBasicUnaryInterceptor(t *testing.T) {
	logger := slog.New(slog.NewTextHandler(os.Stdout, nil))
	interceptor := BasicAuthUnaryInterceptor("user", "pass", logger)

	tests := []struct {
		name            string
		authHeader      string
		includeAuth     bool
		expectedCode    codes.Code
		expectedMessage string
		expectSuccess   bool
	}{
		{
			name:          "valid credentials",
			authHeader:    "Basic " + base64.StdEncoding.EncodeToString([]byte("user:pass")),
			includeAuth:   true,
			expectSuccess: true,
		},
		{
			name:            "missing authorization header",
			includeAuth:     false,
			expectedCode:    codes.Unauthenticated,
			expectedMessage: apperr.ErrAuthMissingAuthorization.Error(),
		},
		{
			name:            "invalid base64",
			authHeader:      "Basic invalidbase64",
			includeAuth:     true,
			expectedCode:    codes.Unauthenticated,
			expectedMessage: apperr.ErrAuthInvalidCredentials.Error(),
		},
		{
			name:            "invalid format",
			authHeader:      "Basic " + base64.StdEncoding.EncodeToString([]byte("invalidformat")),
			includeAuth:     true,
			expectedCode:    codes.Unauthenticated,
			expectedMessage: apperr.ErrAuthInvalidCredentials.Error(),
		},
		{
			name:            "invalid credentials",
			authHeader:      "Basic " + base64.StdEncoding.EncodeToString([]byte("user:wrongpass")),
			includeAuth:     true,
			expectedCode:    codes.Unauthenticated,
			expectedMessage: apperr.ErrAuthInvalidCredentials.Error(),
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			md := metadata.MD{}
			if tt.includeAuth {
				md.Set("authorization", tt.authHeader)
			}
			ctx := metadata.NewIncomingContext(context.Background(), md)

			resp, err := interceptor(ctx, nil, &grpc.UnaryServerInfo{FullMethod: "/test"}, func(ctx context.Context, req interface{}) (interface{}, error) {
				return "success", nil
			})

			if tt.expectSuccess {
				if err != nil {
					t.Fatalf("expected no error, got %v", err)
				}
				if resp != "success" {
					t.Fatalf("expected response 'success', got %v", resp)
				}
				return
			}

			if err == nil {
				t.Fatalf("expected error, got nil")
			}

			if status.Code(err) != tt.expectedCode {
				t.Fatalf("expected code %v, got %v", tt.expectedCode, status.Code(err))
			}

			if status.Convert(err).Message() != tt.expectedMessage {
				t.Fatalf("expected message %q, got %q", tt.expectedMessage, status.Convert(err).Message())
			}
		})
	}
}
