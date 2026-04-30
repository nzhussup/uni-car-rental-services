# Currency Converter gRPC Service

This service exposes a gRPC API for currency exchange rate lookup and amount conversion.
It uses the ECB daily XML feed as the exchange-rate source:

- http://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml

## API Contract

Protocol buffer definition:

- proto/currency_converter.proto

Service:

- CurrencyConverter

RPC methods:

- GetExchangeRate
- ConvertAmount
- GetSupportedCurrencies

## Authentication

Unary requests are protected by Basic authorization metadata via server interceptor.

Set credentials through environment variables before startup:

- SOAP_USERNAME
- SOAP_PASSWORD
- REDIS_ADDR
- REDIS_PASSWORD
- REDIS_TLS_ENABLED

Example:

```bash
export SOAP_USERNAME=admin
export SOAP_PASSWORD=admin
export REDIS_ADDR=localhost:6379
export REDIS_PASSWORD=redispassword1!
export REDIS_TLS_ENABLED=false
```

## Run

From CurrencyConverterService:

```bash
go run ./cmd/server
```

Server listens on :8080.

## Generate gRPC Stubs

If you modify proto/currency_converter.proto, regenerate stubs:

```bash
./generate-server-stub.sh
```

Prerequisites:

- protoc
- protoc-gen-go
- protoc-gen-go-grpc

## Run Tests

```bash
go test ./...
```
