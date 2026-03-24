
using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models.DTOs;

public class CreateBookingDto
{
    public int CarId { get; set; }
    [Required]
    public required DateTime PickupDate { get; set; }
    [Required]
    public required DateTime DropoffDate { get; set; }
}