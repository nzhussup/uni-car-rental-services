namespace CarService.Common;

public class BookingInfo
{
    public int CarId { get; set; }

    public int BookingId { get; set; }

    public DateTime PickupDate { get; set; }

    public DateTime DropoffDate { get; set; }

    public BookingType Type { get; set; }
}