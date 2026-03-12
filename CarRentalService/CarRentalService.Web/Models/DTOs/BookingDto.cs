using CarRentalService.Data.Entities;

namespace CarRentalService.Models.DTOs;

public class BookingDto
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public int UserId { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime PickupDate { get; set; }
    public DateTime DropoffDate { get; set; }
    public decimal TotalCostInUsd { get; set; }
    public BookingStatus Status { get; set; }
}