package ecb

import "encoding/xml"

type RatesData struct {
	Date   string
	Source string
	Rates  map[string]float64
}

type envelope struct {
	XMLName xml.Name `xml:"Envelope"`
	Cube    cubeRoot `xml:"Cube"`
}

type cubeRoot struct {
	Daily dailyCube `xml:"Cube"`
}

type dailyCube struct {
	Time  string     `xml:"time,attr"`
	Rates []rateCube `xml:"Cube"`
}

type rateCube struct {
	Currency string `xml:"currency,attr"`
	Rate     string `xml:"rate,attr"`
}
