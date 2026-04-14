using System.ComponentModel.DataAnnotations;

namespace CarService.Models.DTOs;

public class UpdateCarDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Make { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Model { get; init; }

    [Required]
    [Range(1900, 2100, ErrorMessage = "Year must be between 1900 and 2100")]
    public int Year { get; init; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal PriceInUsd { get; init; }
}