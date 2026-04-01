using CarRentalService.CurrencyConverterService;
using CarRentalService.Models.DTOs;
using CarRentalService.Models.Responses;

namespace CarRentalService.Services;

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
            return new PriceDto()
            {
                Amount = (decimal)response.Body.ConvertedAmount,
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