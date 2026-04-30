using System.Text;
using BookingService.Models.DTOs;
using BookingService.Models.Settings;
using Grpc.Core;
using CurrencyConverterClient = CurrencyConverter.Grpc.CurrencyConverter.CurrencyConverterClient;
using ConvertAmountRequest = CurrencyConverter.Grpc.ConvertAmountRequest;

namespace BookingService.Services;

public class ExtCurrencyConvertService(
    CurrencyConverterClient currencyConverterClient,
    CurrencyConverterSettings settings,
    ILogger<ExtCurrencyConvertService> logger)
    : IExtCurrencyConvertService
{
    public async Task<PriceDto> ConvertMoney(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency)
        {
            return new PriceDto
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }

        try
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));

            var metadata = new Metadata
            {
                { "authorization", $"Basic {credentials}" }
            };

            var response = await currencyConverterClient.ConvertAmountAsync(
                new ConvertAmountRequest
                {
                    Amount = decimal.ToDouble(amount),
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency
                },
                headers: metadata);

            var converted = Convert.ToDecimal(response.ConvertedAmount);

            if (converted > 0)
            {
                return new PriceDto
                {
                    Amount = converted,
                    Currency = toCurrency
                };
            }

            logger.LogWarning(
                "Currency conversion returned zero. Falling back to original amount. From={FromCurrency}, To={ToCurrency}",
                fromCurrency,
                toCurrency);

            return new PriceDto
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }
        catch (RpcException e)
        {
            logger.LogError(
                e,
                "Currency could not be converted. From={FromCurrency}, To={ToCurrency}",
                fromCurrency,
                toCurrency);

            return new PriceDto
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "Currency could not be converted. From={FromCurrency}, To={ToCurrency}",
                fromCurrency,
                toCurrency);

            return new PriceDto
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }
    }
}