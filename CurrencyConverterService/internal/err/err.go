package err

import (
	"errors"
	"fmt"

	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
)

var (
	// Currency conversion errors
	ErrMissingCurrency = errors.New("currency code is required")
	ErrInvalidAmount   = errors.New("amount must be greater than zero")
	ErrFetch           = errors.New("fetch rate failed")

	// ECB XML parsing errors
	ErrECBMissingDailyTimestamp = errors.New("ECB XML missing daily rate timestamp")
	ErrECBEmptyCurrencyCode     = errors.New("ECB XML contains empty currency code")
	ErrECBNoExchangeRates       = errors.New("ECB XML contains no exchange rates")

	// Auth parsing errors
	ErrAuthParseNotBasic      = errors.New("authorization header must use Basic scheme")
	ErrAuthParseInvalidBase64 = errors.New("invalid base64 encoding in authorization header")
	ErrAuthParseInvalidFormat = errors.New("invalid basic credentials format")

	// Auth errors
	ErrAuthInvalidCredentials   = errors.New("invalid username or password")
	ErrAuthMissingMetadata      = errors.New("missing metadata in context")
	ErrAuthMissingAuthorization = errors.New("missing authorization header in metadata")
)

type UnsupportedCurrencyError struct {
	Currency string
}

func (e UnsupportedCurrencyError) Error() string {
	return fmt.Sprintf("unsupported currency: %s", e.Currency)
}

func BuildErrorResponse(err error) error {
	code := codes.Internal
	switch {
	case errors.Is(err, ErrFetch):
		code = codes.FailedPrecondition
	case errors.Is(err, ErrInvalidAmount), errors.Is(err, ErrMissingCurrency):
		code = codes.InvalidArgument
	default:
		var unsupported UnsupportedCurrencyError
		if errors.As(err, &unsupported) {
			code = codes.InvalidArgument
		}
	}
	return status.Error(code, err.Error())
}
