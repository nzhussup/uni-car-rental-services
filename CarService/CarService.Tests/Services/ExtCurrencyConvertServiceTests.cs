using System.ServiceModel;
using CarService.CurrencyConverterService;
using CarService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarService.Tests.Services;

public class ExtCurrencyConvertServiceTests
{
    [Fact]
    public async Task ConvertMoney_ShouldReturnConvertedAmount_WhenServiceSucceeds()
    {
        var channelMock = new Mock<CurrencyConverterPortType>();
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();
        var expectedConvertedAmount = 123.45;

        channelMock
            .Setup(channel => channel.ConvertAmountAsync(It.IsAny<ConvertAmountRequest>()))
            .ReturnsAsync(new ConvertAmountResponse
            {
                Body = new ConvertAmountResponseBody
                {
                    ConvertedAmount = expectedConvertedAmount
                }
            });

        var service = CreateService(channelMock, loggerMock);

        var result = await service.ConvertMoney(100m, "USD", "EUR");

        result.Should().NotBeNull();
        result.Amount.Should().Be((decimal)expectedConvertedAmount);
        result.Currency.Should().Be("EUR");

        channelMock.Verify(channel => channel.ConvertAmountAsync(It.Is<ConvertAmountRequest>(request =>
            request.Body.Amount.Equals(100d) &&
            request.Body.FromCurrency == "USD" &&
            request.Body.ToCurrency == "EUR"
        )), Times.Once);
    }

    [Fact]
    public async Task ConvertMoney_ShouldReturnOriginalAmount_WhenCurrenciesMatch()
    {
        var channelMock = new Mock<CurrencyConverterPortType>();
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();

        var service = CreateService(channelMock, loggerMock);

        var result = await service.ConvertMoney(25m, "USD", "USD");

        result.Should().NotBeNull();
        result.Amount.Should().Be(25m);
        result.Currency.Should().Be("USD");

        channelMock.Verify(channel => channel.ConvertAmountAsync(It.IsAny<ConvertAmountRequest>()), Times.Never);
    }

    [Fact]
    public async Task ConvertMoney_ShouldReturnOriginalAmount_WhenServiceThrows()
    {
        var channelMock = new Mock<CurrencyConverterPortType>();
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();

        channelMock
            .Setup(channel => channel.ConvertAmountAsync(It.IsAny<ConvertAmountRequest>()))
            .ThrowsAsync(new Exception("boom"));

        var service = CreateService(channelMock, loggerMock);

        var result = await service.ConvertMoney(42m, "USD", "EUR");

        result.Should().NotBeNull();
        result.Amount.Should().Be(42m);
        result.Currency.Should().Be("USD");

        loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Currency could not be converted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ConvertMoney_ShouldReturnOriginalAmount_WhenServiceReturnsZero()
    {
        var channelMock = new Mock<CurrencyConverterPortType>();
        var loggerMock = new Mock<ILogger<ExtCurrencyConvertService>>();

        channelMock
            .Setup(channel => channel.ConvertAmountAsync(It.IsAny<ConvertAmountRequest>()))
            .ReturnsAsync(new ConvertAmountResponse
            {
                Body = new ConvertAmountResponseBody
                {
                    ConvertedAmount = 0
                }
            });

        var service = CreateService(channelMock, loggerMock);

        var result = await service.ConvertMoney(17m, "USD", "EUR");

        result.Should().NotBeNull();
        result.Amount.Should().Be(17m);
        result.Currency.Should().Be("USD");

        loggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Currency conversion returned zero")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static ExtCurrencyConvertService CreateService(
        Mock<CurrencyConverterPortType> channelMock,
        Mock<ILogger<ExtCurrencyConvertService>> loggerMock)
    {
        var client = new TestCurrencyConverterClient(channelMock.Object);
        return new ExtCurrencyConvertService(client, loggerMock.Object);
    }

    private sealed class TestCurrencyConverterClient : CurrencyConverterPortTypeClient
    {
        private readonly CurrencyConverterPortType _channel;

        public TestCurrencyConverterClient(CurrencyConverterPortType channel)
            : base(new BasicHttpBinding(), new EndpointAddress("http://localhost"))
        {
            _channel = channel;
        }

        protected override CurrencyConverterPortType CreateChannel()
        {
            return _channel;
        }
    }
}
