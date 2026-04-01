using CarRentalService.Data.Entities;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Models.Responses;

public class BookingResponse
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Guid UserId { get; set; }
    public UserDto User { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime PickupDate { get; set; }
    public DateTime DropoffDate { get; set; }
    public PriceDto TotalCost { get; set; }
    public BookingStatus Status { get; set; }
}