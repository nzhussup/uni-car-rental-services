using BookingService.CurrencyConverterService;
using BookingService.Models.DTOs;

namespace BookingService.Services;

public class ExtCurrencyConvertService(
    CurrencyConverterPortTypeClient currencyConverterClient,
    ILogger<ExtCurrencyConvertService> logger)
    : IExtCurrencyConvertService
{
    public async Task<PriceDto> ConvertMoney(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency)
        {
            return new PriceDto()
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }

        try
        {
            var response = await currencyConverterClient.ConvertAmountAsync((double)amount, fromCurrency, toCurrency);
            var converted = (decimal?)response?.Body?.ConvertedAmount ?? 0m;

            if (converted > 0)
            {
                return new PriceDto()
                {
                    Amount = converted,
                    Currency = toCurrency
                };
            }

            logger.LogWarning("Currency conversion returned zero. Falling back to original amount. From={FromCurrency}, To={ToCurrency}", fromCurrency, toCurrency);
            return new PriceDto()
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }
        catch (Exception e)
        {
            logger.LogError("Currency could not be converted: {message}", e.Message);
            return new PriceDto()
            {
                Amount = amount,
                Currency = fromCurrency
            };
        }

    }
}
