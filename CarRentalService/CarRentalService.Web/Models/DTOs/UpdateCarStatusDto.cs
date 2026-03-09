using System.ComponentModel.DataAnnotations;
using CarRentalService.Data.Entities;

namespace CarRentalService.Models.DTOs;

public class UpdateCarStatusDto
{
    [Required]
    [Range((int)CarStatus.Available, (int)CarStatus.Maintenance, ErrorMessage = "Invalid car status value.")]
    public CarStatus Status { get; init; }
}