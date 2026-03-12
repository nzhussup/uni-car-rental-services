
namespace CarRentalService.Models.DTOs;

public class CreateBookingDto
{
    //TODO: Get UserId from Auth
    public int UserId { get; set; }
    public int CarId { get; set; }
    public required DateTime PickupDate { get; set; }
    public required DateTime DropoffDate { get; set; }
}