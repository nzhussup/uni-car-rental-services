using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Services;

public class BookingService(IBookingRepository repository, ICarRepository carRepository, IMapper mapper, IUserService userService) : IBookingService
{
    private static readonly BookingStatus[] NonBlockingBookingStatuses = [BookingStatus.Canceled, BookingStatus.Completed];

    public async Task<QueryResponse<BookingDto>> GetAllBookingsAsync(PaginationDto pagination)
    {
        var bookings = (await repository.GetAllAsync())
            .OrderByDescending(booking => booking.BookingDate)
            .ThenByDescending(booking => booking.Id);
        var bookingsDto = mapper.Map<IEnumerable<BookingDto>>(bookings.Skip(pagination.Skip).Take(pagination.Take));

        foreach (var bookingDto in bookingsDto)
        {
            bookingDto.User = await userService.GetUserByIdAsync(bookingDto.UserId);
        }

        return new QueryResponse<BookingDto>()
        {
            TotalElements = bookings.Count(),
            Elements = bookingsDto
        };
    }

    public async Task<QueryResponse<BookingDto>> GetAllUserBookingsAsync(Guid userId, PaginationDto pagination)
    {
        var bookings = (await repository.GetAllAsync())
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.BookingDate)
            .ThenByDescending(booking => booking.Id);
        var userDto = await userService.GetUserByIdAsync(userId);
        var bookingsDto = mapper.Map<IEnumerable<BookingDto>>(bookings.Skip(pagination.Skip).Take(pagination.Take));

        foreach (var bookingDto in bookingsDto)
        {
            bookingDto.User = userDto;
        }

        return new QueryResponse<BookingDto>()
        {
            TotalElements = bookings.Count(),
            Elements = bookingsDto
        };
    }

    public async Task<BookingDto> GetBookingByIdAsync(int id)
    {
        var booking = await repository.GetByIdAsync(id);
        return booking == null ? throw new NotFoundException("Booking", id) : mapper.Map<BookingDto>(booking);
    }

    public async Task<BookingDto> GetBookingByIdAsync(Guid userId, int id)
    {
        var booking = await repository.GetByIdAsync(id);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (booking.UserId != userId)
        {
            throw new NotAllowedException("Booking doesnt belong to user.");
        }

        var bookingDto = mapper.Map<BookingDto>(booking);
        var userDto = await userService.GetUserByIdAsync(userId);
        bookingDto.User = userDto;
        return bookingDto;
    }

    public async Task<BookingDto> CreateBookingAsnyc(Guid userid, CreateBookingDto createBookingDto)
    {
        ArgumentNullException.ThrowIfNull(createBookingDto);

        var car = await carRepository.GetByIdAsync(createBookingDto.CarId);
        if (car == null)
        {
            throw new NotFoundException("Car", createBookingDto.CarId);
        }

        if (car.Status == CarStatus.Maintenance)
        {
            throw new NotAllowedException("Cannot book a car that is under maintenance.");
        }

        var overlapping = (await repository.GetAllAsync()).Any(b =>
            b.CarId == createBookingDto.CarId
            && !NonBlockingBookingStatuses.Contains(b.Status)
            && createBookingDto.PickupDate < b.DropoffDate
            && createBookingDto.DropoffDate > b.PickupDate);

        if (overlapping)
        {
            throw new NotAllowedException("Car is already booked for the selected dates.");
        }

        var booking = mapper.Map<Booking>(createBookingDto);
        booking.UserId = userid;
        var days = Math.Max(1, (createBookingDto.DropoffDate - createBookingDto.PickupDate).Days);
        booking.TotalCostInUsd = car.PriceInUsd * days;
        var createdBooking = await repository.AddAsync(booking);
        return mapper.Map<BookingDto>(createdBooking);
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        var result = await repository.DeleteAsync(id);
        return !result ? throw new NotFoundException("Booking", id) : true;
    }

    public async Task<BookingDto> SetBookingStatusAsync(int id, BookingStatus bookingStatus)
    {
        var booking = await repository.GetByIdAsync(id);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (booking.Status == BookingStatus.Completed)
        {
            throw new NotAllowedException("Cant change status of a completed booking.");
        }

        switch (bookingStatus)
        {
            case BookingStatus.Booked:
                booking.Status = BookingStatus.Booked;
                break;
            case BookingStatus.Canceled:
                booking.Status = BookingStatus.Canceled;
                break;
            case BookingStatus.PickedUp:
                booking.Status = BookingStatus.PickedUp;
                break;
            case BookingStatus.ReturnLate:
                booking.Status = BookingStatus.ReturnLate;
                break;
            case BookingStatus.Completed:
                booking.Status = BookingStatus.Completed;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bookingStatus), bookingStatus, null);
        }
        await repository.SaveChangesAsync();
        return mapper.Map<BookingDto>(booking);
    }

    public async Task<BookingDto> CancelBookingAsync(Guid userId, int id)
    {
        var booking = await repository.GetByIdAsync(id);
        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        if (booking.UserId != userId)
        {
            throw new NotAllowedException("Booking doesnt belong to user.");
        }

        if (booking.Status != BookingStatus.Booked)
        {
            throw new NotAllowedException("Cant change status of booking.");
        }

        booking.Status = BookingStatus.Canceled;
        await repository.SaveChangesAsync();
        return mapper.Map<BookingDto>(booking);
    }
}
