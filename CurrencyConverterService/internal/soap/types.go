package soap

import "encoding/xml"

type GetExchangeRateRequest struct {
	XMLName      xml.Name `xml:"GetExchangeRateRequest"`
	FromCurrency string   `xml:"FromCurrency"`
	ToCurrency   string   `xml:"ToCurrency"`
}

type GetExchangeRateResponse struct {
	XMLName        xml.Name `xml:"cur:GetExchangeRateResponse"`
	Rate           float64  `xml:"cur:Rate"`
	Source         string   `xml:"cur:Source"`
	BaseCurrency   string   `xml:"cur:BaseCurrency"`
	TargetCurrency string   `xml:"cur:TargetCurrency"`
}

type ConvertAmountRequest struct {
	XMLName      xml.Name `xml:"ConvertAmountRequest"`
	Amount       float64  `xml:"Amount"`
	FromCurrency string   `xml:"FromCurrency"`
	ToCurrency   string   `xml:"ToCurrency"`
}

type ConvertAmountResponse struct {
	XMLName         xml.Name `xml:"cur:ConvertAmountResponse"`
	ConvertedAmount float64  `xml:"cur:ConvertedAmount"`
	Rate            float64  `xml:"cur:Rate"`
	Source          string   `xml:"cur:Source"`
	BaseCurrency    string   `xml:"cur:BaseCurrency"`
	TargetCurrency  string   `xml:"cur:TargetCurrency"`
}

type GetSupportedCurrenciesRequest struct {
	XMLName xml.Name `xml:"GetSupportedCurrenciesRequest"`
}

type GetSupportedCurrenciesResponse struct {
	XMLName    xml.Name `xml:"cur:GetSupportedCurrenciesResponse"`
	Currencies []string `xml:"Currencies>Currency"`
}
