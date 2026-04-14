using CarRentalService.Data.Entities;
using CarService.Models.DTOs;

namespace CarService.Models.Responses;

public class CarResponse
{
    public PriceDto Price { get; set; }
    public int Id { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public int Year { get; init; }
    public CarStatus Status { get; init; }
}