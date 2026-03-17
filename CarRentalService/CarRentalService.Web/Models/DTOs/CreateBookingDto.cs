
using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models.DTOs;

public class CreateBookingDto
{
    //TODO: Get UserId from Auth
    public int UserId { get; set; }
    public int CarId { get; set; }
    [Required]
    public required DateTime PickupDate { get; set; }
    [Required]
    public required DateTime DropoffDate { get; set; }
}