namespace BookingService.Common;

public class CarInfo
{
    public int CarId { get; set; }

    public int BookingId { get; set; }

    public string Make { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int Year { get; init; }

    public decimal PriceInUsd { get; init; }

    public bool IsAvailable { get; set; }
}