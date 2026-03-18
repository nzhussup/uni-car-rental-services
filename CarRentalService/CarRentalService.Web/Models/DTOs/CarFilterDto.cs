using CarRentalService.Data.Entities;

namespace CarRentalService.Models.DTOs;

public class CarFilterDto
{
    public string? CarManufacturer { get; set; }
    public string? CarModel { get; set; }
    public int? Year { get; set; }
    public CarStatus? Status { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? DropoffDate { get; set; }
}
