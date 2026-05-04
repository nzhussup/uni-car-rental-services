# Currency Converter Service Documentation

## Overview and Responsibility

`CurrencyConverterService` is the dedicated currency backend used by the car and booking APIs. It exposes gRPC operations, protects them with basic-auth interception, reads rates from the European Central Bank XML feed, and optionally caches fetched rates in Redis.

## Tools and Technologies Used

- Go service
- gRPC
- Redis for optional caching
- ECB XML daily feed as external rate source
- Docker for packaging

## API Description

Protocol: gRPC on port `8080`

Defined in [`proto/currency_converter.proto`](./proto/currency_converter.proto):

| RPC | Purpose |
| --- | --- |
| `GetExchangeRate` | Return the exchange rate for one currency pair |
| `ConvertAmount` | Convert an amount between currencies |
| `GetSupportedCurrencies` | Return the set of currencies currently supported by the converter |

Request/response model:

```text
ConvertAmountRequest
|- amount
|- from_currency
`- to_currency

ConvertAmountResponse
|- converted_amount
|- rate
|- source
|- base_currency
`- target_currency
```

## Runtime Wiring and Dependencies

```text
CarService ------\
                  -> CurrencyConverterService -> ECB daily XML feed
BookingService --/

CurrencyConverterService -> Redis (optional cache)
```

Runtime configuration:

- `SOAP_USERNAME`
- `SOAP_PASSWORD`
- `REDIS_ADDR`
- `REDIS_PASSWORD`
- `REDIS_TLS_ENABLED`

Despite the historical `SOAP_` variable names, the current runtime path in this repository is gRPC guarded by a unary interceptor.

## Validation and Error Handling

- service startup exits if `SOAP_USERNAME` or `SOAP_PASSWORD` are missing
- Redis is optional; if it is unavailable, the service logs a warning and continues without cache
- the gRPC interceptor enforces basic authentication before handler execution
- external rate fetching depends on the ECB feed being reachable and well-formed

## Code Quality Practices

- Contract-first API definition in `proto/currency_converter.proto`
- Go module with normal `go test ./...` support
- Backend CI matrix includes this service for quality, tests, and image build
- Redis caching logic is isolated behind a fetcher abstraction

## Lessons Learned

### What does not work yet

- The service has no local fallback data source if the ECB feed is unavailable.
- Variable names still reflect an older SOAP-oriented naming convention.
- There is no end-user API surface; failures only show up through dependent services.

### Experience with technologies

- A small dedicated converter keeps currency logic out of the domain APIs.
- Redis adds useful protection against repeated external fetches with limited complexity.
- gRPC is a better fit for backend-to-backend calls here than exposing another public REST surface.

### Experience with AI tools used

- AI support helped with overall structure and service decomposition ideas.
- The final documentation still required checking the real proto contract and startup code.
