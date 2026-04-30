using BookingService.Models.Settings;
using BookingService.Services;
using CurrencyConverter.Grpc;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using CurrencyConverterClient = CurrencyConverter.Grpc.CurrencyConverter.CurrencyConverterClient;

namespace BookingService.Tests.Services;

public class ExtCurrencyConvertServiceTests
{
    [Fact]
    public async Task ConvertMoney_ShouldReturnConvertedAmount_WhenServiceSucceeds()
    {
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();
        var expectedConvertedAmount = 123.45;

        var callInvoker = new TestCallInvoker(request =>
            CreateUnaryCall(new ConvertAmountResponse
            {
                ConvertedAmount = expectedConvertedAmount,
                Rate = 1.2345,
                Source = "test",
                BaseCurrency = "USD",
                TargetCurrency = "EUR"
            }));

        var service = CreateService(callInvoker, loggerMock);

        var result = await service.ConvertMoney(100m, "USD", "EUR");

        result.Should().NotBeNull();
        result.Amount.Should().Be((decimal)expectedConvertedAmount);
        result.Currency.Should().Be("EUR");

        callInvoker.CallCount.Should().Be(1);
        callInvoker.LastRequest.Should().NotBeNull();
        callInvoker.LastRequest!.Amount.Should().Be(100d);
        callInvoker.LastRequest.FromCurrency.Should().Be("USD");
        callInvoker.LastRequest.ToCurrency.Should().Be("EUR");
    }

    [Fact]
    public async Task ConvertMoney_ShouldReturnOriginalAmount_WhenCurrenciesMatch()
    {
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();

        var callInvoker = new TestCallInvoker(_ =>
            CreateUnaryCall(new ConvertAmountResponse
            {
                ConvertedAmount = 999,
                TargetCurrency = "EUR"
            }));

        var service = CreateService(callInvoker, loggerMock);

        var result = await service.ConvertMoney(25m, "USD", "USD");

        result.Should().NotBeNull();
        result.Amount.Should().Be(25m);
        result.Currency.Should().Be("USD");

        callInvoker.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ConvertMoney_ShouldReturnOriginalAmount_WhenServiceThrows()
    {
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();

        var callInvoker = new TestCallInvoker(_ =>
            CreateFailedUnaryCall<ConvertAmountResponse>(new Exception("boom")));

        var service = CreateService(callInvoker, loggerMock);

        var result = await service.ConvertMoney(42m, "USD", "EUR");

        result.Should().NotBeNull();
        result.Amount.Should().Be(42m);
        result.Currency.Should().Be("USD");

        loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Currency could not be converted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConvertMoney_ShouldReturnOriginalAmount_WhenServiceReturnsZero()
    {
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();

        var callInvoker = new TestCallInvoker(request =>
            CreateUnaryCall(new ConvertAmountResponse
            {
                ConvertedAmount = 0,
                Rate = 0,
                Source = "test",
                BaseCurrency = request.FromCurrency,
                TargetCurrency = request.ToCurrency
            }));

        var service = CreateService(callInvoker, loggerMock);

        var result = await service.ConvertMoney(17m, "USD", "EUR");

        result.Should().NotBeNull();
        result.Amount.Should().Be(17m);
        result.Currency.Should().Be("USD");

        loggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("Currency conversion returned zero")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static ExtCurrencyConvertService CreateService(
        TestCallInvoker callInvoker,
        Mock<ILogger<ExtCurrencyConvertService>> loggerMock)
    {
        var client = new CurrencyConverterClient(callInvoker);

        var settings = new CurrencyConverterSettings
        {
            GrpcUrl = "http://localhost:8080",
            Username = "admin",
            Password = "admin"
        };

        return new ExtCurrencyConvertService(client, settings, loggerMock.Object);
    }

    private static AsyncUnaryCall<TResponse> CreateUnaryCall<TResponse>(TResponse response)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }

    private static AsyncUnaryCall<TResponse> CreateFailedUnaryCall<TResponse>(Exception exception)
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromException<TResponse>(exception),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.Internal, exception.Message),
            () => new Metadata(),
            () => { });
    }

    private sealed class TestCallInvoker(
        Func<ConvertAmountRequest, AsyncUnaryCall<ConvertAmountResponse>> convertAmountHandler)
        : CallInvoker
    {
        public ConvertAmountRequest? LastRequest { get; private set; }

        public int CallCount { get; private set; }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request)
        {
            if (request is ConvertAmountRequest convertAmountRequest &&
                typeof(TResponse) == typeof(ConvertAmountResponse))
            {
                CallCount++;
                LastRequest = convertAmountRequest;

                return (AsyncUnaryCall<TResponse>)(object)convertAmountHandler(convertAmountRequest);
            }

            throw new NotSupportedException($"Unexpected gRPC call: {method.FullName}");
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request)
        {
            throw new NotSupportedException();
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options)
        {
            throw new NotSupportedException();
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options,
            TRequest request)
        {
            throw new NotSupportedException();
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method,
            string? host,
            CallOptions options)
        {
            throw new NotSupportedException();
        }
    }
}