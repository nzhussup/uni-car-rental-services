using CarRentalService.Models.DTOs;
using CarRentalService.Models.Responses;

namespace CarRentalService.Services;

public interface IExtCurrencyConvertService
{
    Task<PriceDto> ConvertMoney(decimal amount, string fromCurrency, string toCurrency);
}