using CarService.Models.DTOs;

namespace CarService.Services;

public interface IExtCurrencyConvertService
{
    Task<PriceDto> ConvertMoney(decimal amount, string fromCurrency, string toCurrency);
}