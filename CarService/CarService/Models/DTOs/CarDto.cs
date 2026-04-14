using CarRentalService.Data.Entities;

namespace CarService.Models.DTOs;

public class CarDto
{
    public int Id { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public int Year { get; init; }
    public decimal PriceInUsd { get; init; }
    public CarStatus Status { get; init; }
}