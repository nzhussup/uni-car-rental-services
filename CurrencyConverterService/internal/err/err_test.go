package err

import (
	"errors"
	"testing"

	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

func TestBuildErrorRespons(t *testing.T) {
	tests := []struct {
		name string
		err  error
		want codes.Code
	}{
		{
			name: "fetch error",
			err:  ErrFetch,
			want: codes.FailedPrecondition,
		},
		{
			name: "invalid amount",
			err:  ErrInvalidAmount,
			want: codes.InvalidArgument,
		},
		{
			name: "missing currency",
			err:  ErrMissingCurrency,
			want: codes.InvalidArgument,
		},
		{
			name: "unsupported currency",
			err:  UnsupportedCurrencyError{Currency: "XYZ"},
			want: codes.InvalidArgument,
		},
		{
			name: "other error",
			err:  errors.New("some other error"),
			want: codes.Internal,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			err := BuildErrorResponse(tt.err)
			st, ok := status.FromError(err)
			if !ok {
				t.Fatalf("expected a gRPC status error, got: %v", err)
			}
			if st.Code() != tt.want {
				t.Errorf("expected code %v, got %v", tt.want, st.Code())
			}
			if st.Message() != tt.err.Error() {
				t.Errorf("expected message %q, got %q", tt.err.Error(), st.Message())
			}
		})
	}
}
