using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Data.Entities;

public class DateRange
{
    public long Id { get; init; }

    public Car Car { get; init; }

    public int CarId { get; init; }

    public int BookingId { get; init; }

    [Required]
    public DateTime PickupDate { get; init; }

    [Required]
    public DateTime DropOffDate { get; init; }
}