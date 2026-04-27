using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarService.Common;
using CarService.Exceptions;
using CarService.Models.DTOs;
using CarService.Models.Responses;

namespace CarService.Services;

public class CarService(ICarRepository carRepository, IMapper mapper, IMessageProducer messageProducer) : ICarService
{
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
            cars = cars.Where(c => c.Status == CarStatus.Available);

            // TODO: What to do about this?
            // Unavailable Dates will be send by booking service via rabbit mq
            cars = cars.Where(c => !c.UnavailableDates.Any(b =>
                filter.PickupDate.Value.Date < b.DropOffDate && filter.DropoffDate.Value.Date > b.PickupDate));
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

        if (!result)
        {
            throw new NotFoundException("Car", id);
        }

        await messageProducer.SendMaintenanceInfoMessageAsync(new MaintainanceStartInfo()
        {
            CarId = id,
            StartDate = DateTime.Now.Date,
        });

        return true;
    }

    public async Task<CarDto> SetCarStatusAsync(int id, CarStatus status)
    {
        var car = await carRepository.GetByIdAsync(id);
        if (car == null)
            throw new NotFoundException("Car", id);

        car.Status = status;
        if (car.Status == CarStatus.Maintenance)
        {
            // We don't remove the unavailable dates here since we let the booking controller handle this
            await messageProducer.SendMaintenanceInfoMessageAsync(new MaintainanceStartInfo()
            {
                CarId = car.Id,
                StartDate = DateTime.Now.Date,
            });
        }
        await carRepository.SaveChangesAsync();
        return mapper.Map<CarDto>(car);
    }

    public async Task HandleBookingInfoAsync(BookingInfo bookingInfo)
    {
        var car = await carRepository.GetByIdAsync(bookingInfo.CarId);
        if (car == null)
        {
            // TODO: Don't know what to do if a car doesnt exist, since we cant really throw an exception
            await messageProducer.SendCarInfoMessageAsync(new CarInfo() { CarId = bookingInfo.CarId, BookingId = bookingInfo.BookingId, IsAvailable = false });
            return;
        }

        var carInfo = mapper.Map<CarInfo>(car);
        carInfo.BookingId = bookingInfo.BookingId;

        // CHeck if var is available and not in maintainance and add the dates to unavailable dates and send back info about the car
        if (bookingInfo.Type == BookingType.Check && car.Status == CarStatus.Available)
        {
            await carRepository.AddUnavailableDateRangeAsync(car, bookingInfo.BookingId, bookingInfo.PickupDate, bookingInfo.DropoffDate);
            carInfo.IsAvailable = true;
            await messageProducer.SendCarInfoMessageAsync(carInfo);
        }

        // If a car is not available simply send back that booking is not valid
        if (bookingInfo.Type == BookingType.Check && car.Status != CarStatus.Available)
        {
            carInfo.IsAvailable = false;
            await messageProducer.SendCarInfoMessageAsync(carInfo);
        }

        // If a booking is canceled remove the unavailable date
        if (bookingInfo.Type == BookingType.Canceled)
        {
            await carRepository.RemoveUnavailableDateRangeAsync(car, bookingInfo.BookingId, bookingInfo.PickupDate, bookingInfo.DropoffDate);
        }
    }
}
