using System.ComponentModel.DataAnnotations;
using CarRentalService.Data.Entities;

namespace BookingService.Models.DTOs;

public class UpdateBookingStatusDto
{
    [Required]
    [Range((int)BookingStatus.Booked, (int)BookingStatus.Completed, ErrorMessage = "Invalid booking status value.")]
    public BookingStatus Status { get; init; }
}