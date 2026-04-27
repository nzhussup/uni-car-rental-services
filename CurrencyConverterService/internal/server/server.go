package server

import (
	"context"
	"fmt"
	"log/slog"
	"net"

	apperr "currency-converter-service/internal/err"
	"currency-converter-service/internal/service"
	currencyconverterpb "currency-converter-service/proto"

	"google.golang.org/grpc"
)

type converterService interface {
	GetExchangeRate(ctx context.Context, fromCurrency, toCurrency string) (service.ExchangeRateResult, error)
	ConvertAmount(ctx context.Context, amount float64, fromCurrency, toCurrency string) (service.ConversionResult, error)
	GetSupportedCurrencies(ctx context.Context) ([]string, error)
}

type Server struct {
	currencyconverterpb.UnimplementedCurrencyConverterServer
	converter  converterService
	logger     *slog.Logger
	grpcServer *grpc.Server
	port       int
}

func NewServer(converter converterService, logger *slog.Logger, port int, opts []grpc.ServerOption) *Server {
	if logger == nil {
		logger = slog.Default()
	}

	grpcServer := grpc.NewServer(opts...)

	srv := &Server{
		converter:  converter,
		logger:     logger,
		grpcServer: grpcServer,
		port:       port,
	}
	currencyconverterpb.RegisterCurrencyConverterServer(grpcServer, srv)
	return srv
}

func (s *Server) Start() error {
	lis, err := net.Listen("tcp", fmt.Sprintf(":%d", s.port))
	if err != nil {
		return fmt.Errorf("failed to listen on port %d: %w", s.port, err)
	}

	s.logger.Info("gRPC server started successfully", "port", s.port)
	if err := s.grpcServer.Serve(lis); err != nil {
		return fmt.Errorf("gRPC server stopped with error: %w", err)
	}
	return nil
}

func (s *Server) Stop() {
	s.logger.Info("stopping gRPC server")
	s.grpcServer.GracefulStop()
}

func (s *Server) GetExchangeRate(ctx context.Context, req *currencyconverterpb.GetExchangeRateRequest) (*currencyconverterpb.GetExchangeRateResponse, error) {
	result, err := s.converter.GetExchangeRate(ctx, req.FromCurrency, req.ToCurrency)
	if err != nil {
		return nil, apperr.BuildErrorResponse(err)
	}
	return &currencyconverterpb.GetExchangeRateResponse{
		Rate: result.Rate,
	}, nil
}

func (s *Server) ConvertAmount(ctx context.Context, req *currencyconverterpb.ConvertAmountRequest) (*currencyconverterpb.ConvertAmountResponse, error) {
	result, err := s.converter.ConvertAmount(ctx, req.Amount, req.FromCurrency, req.ToCurrency)
	if err != nil {
		return nil, apperr.BuildErrorResponse(err)
	}
	return &currencyconverterpb.ConvertAmountResponse{
		ConvertedAmount: result.ConvertedAmount,
	}, nil
}

func (s *Server) GetSupportedCurrencies(ctx context.Context, req *currencyconverterpb.GetSupportedCurrenciesRequest) (*currencyconverterpb.GetSupportedCurrenciesResponse, error) {
	currencies, err := s.converter.GetSupportedCurrencies(ctx)
	if err != nil {
		return nil, apperr.BuildErrorResponse(err)
	}
	return &currencyconverterpb.GetSupportedCurrenciesResponse{
		Currencies: currencies,
	}, nil
}
