using CarRentalService.Data.Entities;

namespace BookingService.Models.DTOs;

public class BookingDto
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public Guid UserId { get; set; }
    public UserDto User { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime PickupDate { get; set; }
    public DateTime DropoffDate { get; set; }
    public decimal TotalCostInUsd { get; set; }
    public BookingStatus Status { get; set; }
}