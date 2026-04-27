using CarRentalService.Data.Entities;
using CarService.Common;
using CarService.Models.DTOs;
using CarService.Models.Responses;

namespace CarService.Services;

public interface ICarService
{
    Task<QueryResponse<CarDto>> GetAllCarsAsync(CarFilterDto? filter, PaginationDto pagination);
    Task<CarDto> GetCarByIdAsync(int id);
    Task<CarDto> CreateCarAsync(CreateCarDto createCarDto);
    Task<CarDto> UpdateCarAsync(int id, UpdateCarDto updateCarDto);
    Task<bool> DeleteCarAsync(int id);
    Task<CarDto> SetCarStatusAsync(int id, CarStatus status);

    Task HandleBookingInfoAsync(BookingInfo bookingInfo);
}