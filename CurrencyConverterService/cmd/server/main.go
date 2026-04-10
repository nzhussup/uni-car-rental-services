package main

import (
	"log/slog"
	"net/http"
	"os"
	"strings"
	"time"

	"currency-converter-service/internal/ecb"
	"currency-converter-service/internal/service"
	"currency-converter-service/internal/soap"
)

const (
	serverAddress = ":8080"

	ecbFeedURL = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml"
	wsdlPath   = "wsdl/currency-converter.wsdl"
)

func main() {
	logger := slog.New(slog.NewTextHandler(os.Stdout, nil))

	httpClient := &http.Client{Timeout: 10 * time.Second}
	ecbClient := ecb.NewClient(ecbFeedURL, httpClient)
	converter := service.NewConverter(ecbClient)
	soapUsername := strings.TrimSpace(os.Getenv("SOAP_USERNAME"))
	soapPassword := strings.TrimSpace(os.Getenv("SOAP_PASSWORD"))
	if soapUsername == "" || soapPassword == "" {
		logger.Error("SOAP_USERNAME and SOAP_PASSWORD environment variables are required")
		os.Exit(1)
	}
	handler := soap.NewHandler(converter, wsdlPath, soapUsername, soapPassword, logger)

	mux := http.NewServeMux()
	mux.HandleFunc("/soap", handler.HandleSOAP)
	mux.HandleFunc("/wsdl", handler.HandleWSDL)
	mux.HandleFunc("/health", handler.HandleHealth)

	server := &http.Server{
		Addr:              serverAddress,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}

	logger.Info("currency converter service started", "address", serverAddress)
	if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		logger.Error("server stopped with error", "error", err)
		os.Exit(1)
	}
}
