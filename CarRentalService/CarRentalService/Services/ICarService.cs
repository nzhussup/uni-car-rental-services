using CarRentalService.Models;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Services;

public interface ICarService
{
    Task<IEnumerable<CarDto>> GetAllCarsAsync();
    Task<CarDto?> GetCarByIdAsync(int id);
    Task<CarDto> CreateCarAsync(CreateCarDto createCarDto);
    Task<CarDto?> UpdateCarAsync(int id, UpdateCarDto updateCarDto);
    Task<bool> DeleteCarAsync(int id);
    Task<CarDto?> SetCarStatusAsync(int id, CarStatus status);
}