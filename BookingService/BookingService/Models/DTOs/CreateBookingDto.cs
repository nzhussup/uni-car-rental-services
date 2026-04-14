
using System.ComponentModel.DataAnnotations;

namespace BookingService.Models.DTOs;

public class CreateBookingDto
{
    public int CarId { get; set; }
    [Required]
    public required DateTime PickupDate { get; set; }
    [Required]
    public required DateTime DropoffDate { get; set; }
}