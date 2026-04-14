namespace BookingService.Models.DTOs;

public class PriceDto
{
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
}