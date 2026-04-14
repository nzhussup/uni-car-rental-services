using System.ComponentModel.DataAnnotations;
using CarRentalService.Data.Entities;

namespace CarService.Models.DTOs;

public class UpdateCarStatusDto
{
    [Required]
    [EnumDataType(typeof(CarStatus), ErrorMessage = "Invalid car status value.")]
    public CarStatus Status { get; init; }
}
