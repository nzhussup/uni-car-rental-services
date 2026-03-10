# Currency Converter SOAP Service

This service exposes a SOAP 1.1 API for currency exchange rates and amount conversion.
It uses the ECB daily XML feed as the exchange-rate source:

- `http://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml`

## Endpoints

- `POST /soap` SOAP operations (Basic Auth required)
- `GET /wsdl` service contract
- `GET /health` health check

## Authentication

Basic Auth credentials (v1 hardcoded):

- Username: `admin`
- Password: `admin`

## Run

From `CurrencyConverterService/`:

```bash
go run ./cmd/server
```

Server starts on `:8080`.

## Example SOAP request

`ConvertAmount`:

```xml
<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:cur="http://example.com/currencyconverter">
	<soapenv:Body>
		<cur:ConvertAmountRequest>
			<Amount>100</Amount>
			<FromCurrency>USD</FromCurrency>
			<ToCurrency>CHF</ToCurrency>
		</cur:ConvertAmountRequest>
	</soapenv:Body>
</soapenv:Envelope>
```
