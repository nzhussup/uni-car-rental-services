using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalService.Data.Entities;

public class Booking
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public Car Car { get; set; }

    public long UserId { get; set; }

    public User User { get; set; }

    public DateTime BookingDate { get; set; } = DateTime.Now;

    [Required]
    public DateTime PickupDate { get; set; }

    [Required]
    public DateTime DropoffDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCostInUsd { get; set; }

    [Required] public BookingStatus Status { get; set; } = BookingStatus.Booked;
}