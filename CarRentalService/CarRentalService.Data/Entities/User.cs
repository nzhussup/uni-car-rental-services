using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Data.Entities;

public class User
{
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Email { get; set; }

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    public HashSet<Booking> Bookings { get; set; }
}