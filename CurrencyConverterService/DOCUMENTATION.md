# CurrencyConverterService API Documentation

## Overview

`CurrencyConverterService` is a SOAP 1.1 service used by `CarRentalService` for exchange-rate retrieval and amount conversion.

## Technologies

- Go 1.24
- HTTP + SOAP 1.1
- WSDL contract (`wsdl/currency-converter.wsdl`)
- ECB daily XML feed
- HTTP Basic Auth (`SOAP_USERNAME`, `SOAP_PASSWORD`)

## Endpoints

- `POST /soap` (SOAP operations, authenticated)
- `GET /wsdl` (WSDL)
- `GET /health` (health check)

## SOAP Operations

- `GetExchangeRate(FromCurrency, ToCurrency)`
- `ConvertAmount(Amount, FromCurrency, ToCurrency)`
- `GetSupportedCurrencies()`

## Data Contract (Simplified)

```text
ConvertAmountRequest
- Amount: float
- FromCurrency: string
- ToCurrency: string

ConvertAmountResponse
- ConvertedAmount: float
- Rate: float
- Source: string
- BaseCurrency: string
- TargetCurrency: string
```

## Validation Rules

- Missing currency code -> client error
- Unsupported currency -> client error
- `Amount <= 0` -> client error
- Upstream ECB fetch failures -> server-side operation failure

## Processing Flow

```text
SOAP request
-> Basic Auth check
-> operation parse
-> fetch rates from ECB
-> calculate rate/conversion
-> return SOAP response or SOAP fault
```

## Quality Notes

Test coverage exists for:

- ECB client parsing/validation
- conversion logic
- SOAP handler behavior
- SOAP fault mapping
