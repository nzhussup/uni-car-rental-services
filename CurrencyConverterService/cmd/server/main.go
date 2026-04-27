package main

import (
	"context"
	"log/slog"
	"net/http"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"currency-converter-service/internal/auth"
	"currency-converter-service/internal/ecb"
	"currency-converter-service/internal/server"
	"currency-converter-service/internal/service"

	"google.golang.org/grpc"
)

const (
	serverPort = 8080

	ecbFeedURL = "http://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml"
	protoPath  = "proto/currency_converter.proto"
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

	opts := []grpc.ServerOption{
		grpc.UnaryInterceptor(auth.BasicAuthUnaryInterceptor(soapUsername, soapPassword, logger)),
	}

	srv := server.NewServer(converter, logger, serverPort, opts)

	ctx, stopSig := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stopSig()

	go func() {
		if err := srv.Start(); err != nil {
			logger.Error("failed to start server", "error", err)
			os.Exit(1)
		}
	}()

	<-ctx.Done()
	srv.Stop()
}
