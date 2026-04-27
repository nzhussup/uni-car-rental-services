namespace BookingService.Common;

public class MaintainanceStartInfo
{
    public int CarId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
}