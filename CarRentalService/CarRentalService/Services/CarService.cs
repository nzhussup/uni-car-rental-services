using CarRentalService.Models;
using CarRentalService.Models.DTOs;
using CarRentalService.Repositories;

namespace CarRentalService.Services;

public class CarService(ICarRepository carRepository) : ICarService
{
    public async Task<IEnumerable<CarDto>> GetAllCarsAsync()
    {
        var cars = await carRepository.GetAllAsync();
        return cars.Select(MapToDto);
    }

    public async Task<CarDto?> GetCarByIdAsync(int id)
    {
        var car = await carRepository.GetByIdAsync(id);
        return car == null ? null : MapToDto(car);
    }

    public async Task<CarDto> CreateCarAsync(CreateCarDto createCarDto)
    {
        var car = new Car
        {
            Make = createCarDto.Make,
            Model = createCarDto.Model,
            Year = createCarDto.Year,
            PriceInUsd = createCarDto.PriceInUsd
        };

        var createdCar = await carRepository.AddAsync(car);
        return MapToDto(createdCar);
    }

    public async Task<CarDto?> UpdateCarAsync(int id, UpdateCarDto updateCarDto)
    {
        var car = new Car
        {
            Id = id,
            Make = updateCarDto.Make,
            Model = updateCarDto.Model,
            Year = updateCarDto.Year,
            PriceInUsd = updateCarDto.PriceInUsd
        };

        var updatedCar = await carRepository.UpdateAsync(car);
        return updatedCar == null ? null : MapToDto(updatedCar);
    }

    public async Task<bool> DeleteCarAsync(int id)
    {
        return await carRepository.DeleteAsync(id);
    }

    public async Task<CarDto?> SetCarStatusAsync(int id, CarStatus status)
    {
        var car = await carRepository.GetByIdAsync(id);
        if (car == null) return null;

        car.Status = status;
        var updatedCar = await carRepository.UpdateAsync(car);
        return updatedCar == null ? null : MapToDto(updatedCar);
    }

    private static CarDto MapToDto(Car car)
    {
        return new CarDto
        {
            Id = car.Id,
            Make = car.Make,
            Model = car.Model,
            Year = car.Year,
            PriceInUsd = car.PriceInUsd,
            Status = car.Status
        };
    }
}