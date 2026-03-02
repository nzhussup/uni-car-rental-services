package soap

import "encoding/xml"

const (
	clientFaultCode = "soapenv:Client"
	serverFaultCode = "soapenv:Server"
)

type SOAPFault struct {
	XMLName     xml.Name     `xml:"Fault"`
	FaultCode   string       `xml:"faultcode"`
	FaultString string       `xml:"faultstring"`
	Detail      *FaultDetail `xml:"detail,omitempty"`
}

type FaultDetail struct {
	Message string `xml:"Message"`
}

func NewClientFault(message string) SOAPFault {
	return SOAPFault{
		FaultCode:   clientFaultCode,
		FaultString: message,
		Detail:      &FaultDetail{Message: message},
	}
}

func NewServerFault(message string) SOAPFault {
	return SOAPFault{
		FaultCode:   serverFaultCode,
		FaultString: message,
		Detail:      &FaultDetail{Message: message},
	}
}
