package soap

import "encoding/xml"

type GetExchangeRateRequest struct {
	XMLName      xml.Name `xml:"GetExchangeRateRequest"`
	FromCurrency string   `xml:"FromCurrency"`
	ToCurrency   string   `xml:"ToCurrency"`
}

type GetExchangeRateResponse struct {
	XMLName        xml.Name `xml:"cur:GetExchangeRateResponse"`
	Rate           float64  `xml:"Rate"`
	Source         string   `xml:"Source"`
	BaseCurrency   string   `xml:"BaseCurrency"`
	TargetCurrency string   `xml:"TargetCurrency"`
}

type ConvertAmountRequest struct {
	XMLName      xml.Name `xml:"ConvertAmountRequest"`
	Amount       float64  `xml:"Amount"`
	FromCurrency string   `xml:"FromCurrency"`
	ToCurrency   string   `xml:"ToCurrency"`
}

type ConvertAmountResponse struct {
	XMLName         xml.Name `xml:"cur:ConvertAmountResponse"`
	ConvertedAmount float64  `xml:"ConvertedAmount"`
	Rate            float64  `xml:"Rate"`
	Source          string   `xml:"Source"`
	BaseCurrency    string   `xml:"BaseCurrency"`
	TargetCurrency  string   `xml:"TargetCurrency"`
}

type GetSupportedCurrenciesRequest struct {
	XMLName xml.Name `xml:"GetSupportedCurrenciesRequest"`
}

type GetSupportedCurrenciesResponse struct {
	XMLName    xml.Name `xml:"cur:GetSupportedCurrenciesResponse"`
	Currencies []string `xml:"Currencies>Currency"`
}
