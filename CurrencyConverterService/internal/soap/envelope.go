package soap

import (
	"encoding/xml"
)

const (
	soapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/"
	serviceNamespace      = "http://example.com/currencyconverter"
)

type requestEnvelope struct {
	XMLName xml.Name    `xml:"Envelope"`
	Body    requestBody `xml:"Body"`
}

type requestBody struct {
	GetExchangeRateRequest        *GetExchangeRateRequest        `xml:"GetExchangeRateRequest"`
	ConvertAmountRequest          *ConvertAmountRequest          `xml:"ConvertAmountRequest"`
	GetSupportedCurrenciesRequest *GetSupportedCurrenciesRequest `xml:"GetSupportedCurrenciesRequest"`
}

type responseEnvelope struct {
	XMLName   xml.Name     `xml:"soapenv:Envelope"`
	XMLNSSoap string       `xml:"xmlns:soapenv,attr"`
	XMLNSCur  string       `xml:"xmlns:cur,attr"`
	Body      responseBody `xml:"soapenv:Body"`
}

type responseBody struct {
	InnerXML string `xml:",innerxml"`
}

func buildResponseEnvelope(inner []byte) responseEnvelope {
	return responseEnvelope{
		XMLNSSoap: soapEnvelopeNamespace,
		XMLNSCur:  serviceNamespace,
		Body: responseBody{
			InnerXML: string(inner),
		},
	}
}
