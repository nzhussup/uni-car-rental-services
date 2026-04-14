using BookingService.Models.DTOs;

namespace BookingService.Services;

public interface IExtCurrencyConvertService
{
    Task<PriceDto> ConvertMoney(decimal amount, string fromCurrency, string toCurrency);
}