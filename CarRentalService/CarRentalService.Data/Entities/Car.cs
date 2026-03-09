using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalService.Data.Entities;

public class Car
{
    public int Id { get; set; }

    [Required][MaxLength(100)] public required string Make { get; init; }

    [Required][MaxLength(100)] public required string Model { get; init; }

    [Required] public int Year { get; init; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceInUsd { get; init; }

    [Required] public CarStatus Status { get; set; } = CarStatus.Available;
}