using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Services;

public class CarService(ICarRepository carRepository, IMapper mapper) : ICarService
{
    private static readonly BookingStatus[] NonBlockingBookingStatuses = [BookingStatus.Canceled, BookingStatus.Completed];

    public async Task<QueryResponse<CarDto>> GetAllCarsAsync(CarFilterDto? filter, PaginationDto pagination)
    {
        var cars = await carRepository.GetAllAsync();
        if (filter != null)
        {
            cars = FilterCars(cars, filter);
        }

        return new QueryResponse<CarDto>()
        {
            TotalElements = cars.Count(),
            Elements = mapper.Map<IEnumerable<CarDto>>(cars.Skip(pagination.Skip).Take(pagination.Take))
        };
    }

    private IQueryable<Car> FilterCars(IQueryable<Car> cars, CarFilterDto filter)
    {
        if (!string.IsNullOrEmpty(filter.CarManufacturer))
        {
            cars = cars.Where(x => x.Make.ToLower() == filter.CarManufacturer.ToLower());
        }

        if (!string.IsNullOrEmpty(filter.CarModel))
        {
            cars = cars.Where(x => x.Model.ToLower() == filter.CarModel.ToLower());
        }

        if (filter.Year.HasValue)
        {
            cars = cars.Where(x => x.Year == filter.Year.Value);
        }

        if (filter.Status.HasValue)
        {
            cars = cars.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.PickupDate.HasValue && filter.DropoffDate.HasValue)
        {
            cars = cars.Where(c => c.Status != CarStatus.Maintenance);

            cars = cars.Where(c => !c.CarBookings.Any(b =>
                !NonBlockingBookingStatuses.Contains(b.Status) &&
                filter.PickupDate.Value.Date < b.DropoffDate && filter.DropoffDate.Value.Date > b.PickupDate));
        }

        return cars;
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
