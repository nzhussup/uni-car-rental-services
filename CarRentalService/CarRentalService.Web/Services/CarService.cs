using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Services;

public class CarService(ICarRepository carRepository, IMapper mapper) : ICarService
{
    public async Task<IEnumerable<CarDto>> GetAllCarsAsync()
    {
        var cars = await carRepository.GetAllAsync();
        return mapper.Map<IEnumerable<CarDto>>(cars);
    }

    public async Task<CarDto> GetCarByIdAsync(int id)
    {
        var car = await carRepository.GetByIdAsync(id);
        return car == null ? throw new NotFoundException("Car", id) : mapper.Map<CarDto>(car);
    }

    public async Task<CarDto> CreateCarAsync(CreateCarDto createCarDto)
    {
        var car = mapper.Map<Car>(createCarDto);
        var createdCar = await carRepository.AddAsync(car);
        return mapper.Map<CarDto>(createdCar);
    }

    public async Task<CarDto> UpdateCarAsync(int id, UpdateCarDto updateCarDto)
    {
        var existingCar = await carRepository.GetByIdAsync(id);
        if (existingCar == null)
            throw new NotFoundException("Car", id);

        var car = mapper.Map<Car>(updateCarDto);
        car.Id = id;

        var updatedCar = await carRepository.UpdateAsync(car);
        return mapper.Map<CarDto>(updatedCar);
    }

    public async Task<bool> DeleteCarAsync(int id)
    {
        var result = await carRepository.DeleteAsync(id);
        return !result ? throw new NotFoundException("Car", id) : true;
    }

    public async Task<CarDto> SetCarStatusAsync(int id, CarStatus status)
    {
        var car = await carRepository.GetByIdAsync(id);
        if (car == null)
            throw new NotFoundException("Car", id);

        car.Status = status;
        await carRepository.SaveChangesAsync();
        return mapper.Map<CarDto>(car);
    }
}