using AutoMapper;
using BookingService.Common;
using BookingService.Exceptions;
using BookingService.Models.DTOs;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;

namespace BookingService.Services;

public class BookingService(IBookingRepository repository, IMapper mapper, IUserService userService, IMessageProducer messageProducer) : IBookingService
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
        var createdBooking = await repository.AddAsync(booking);

        // Send booking info to car service 
        await messageProducer.SendBookingInfoAsync(mapper.Map<BookingInfo>(booking));

        // Move to handlecarinfo
        return mapper.Map<BookingDto>(createdBooking);
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        var booking = await repository.GetByIdAsync(id);

        if (booking == null)
        {
            throw new NotFoundException("Booking", id);
        }

        await repository.DeleteAsync(id);
        var bookingInfo = mapper.Map<BookingInfo>(booking);
        bookingInfo.Type = BookingType.Canceled;

        // Incase the booking gets deleted we also want to remove the unavailable dates from the car
        await messageProducer.SendBookingInfoAsync(bookingInfo);

        return true;
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
            case BookingStatus.Canceled:
                booking.Status = BookingStatus.Canceled;
                await messageProducer.SendBookingInfoAsync(mapper.Map<BookingInfo>(booking));
                break;
            case BookingStatus.PickedUp:
                booking.Status = BookingStatus.PickedUp;
                break;
            case BookingStatus.ReturnLate:
                booking.Status = BookingStatus.ReturnLate;
                break;
            case BookingStatus.Completed:
                booking.Status = BookingStatus.Completed;
                await messageProducer.SendBookingInfoAsync(mapper.Map<BookingInfo>(booking));
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

        if (booking.Status == BookingStatus.Canceled)
        {
            throw new NotAllowedException("Cant change status of booking.");
        }

        booking.Status = BookingStatus.Canceled;
        await repository.SaveChangesAsync();
        await messageProducer.SendBookingInfoAsync(mapper.Map<BookingInfo>(booking));
        return mapper.Map<BookingDto>(booking);
    }

    public async Task HandleMaintainanceInfoAsync(MaintainanceStartInfo startInfo)
    {
        var bookings = await repository.GetAllAsync();

        // Get all affected bookings and cancel them#
        // Maybe later we can add like a maintainance start and enddate?
        var bookingsList = bookings.Where(x => x.PickupDate <= startInfo.StartDate && x.CarId == startInfo.CarId).ToList();

        foreach (var booking in bookingsList)
        {
            await this.SetBookingStatusAsync(booking.Id, BookingStatus.Canceled);
        }
    }

    public async Task HandleCarInfoAsync(CarInfo carInfo)
    {
        var booking = await repository.GetByIdAsync(carInfo.BookingId);
        if (booking == null)
        {
            return;
        }

        booking.Make = carInfo.Make;
        booking.Model = carInfo.Model;
        booking.CarPriceInUsd = carInfo.PriceInUsd;
        booking.CarYear = carInfo.Year;
        booking.Status = carInfo.IsAvailable ? BookingStatus.Booked : BookingStatus.Canceled;
        var days = Math.Max(1, (booking.DropoffDate - booking.PickupDate).Days);
        booking.TotalCostInUsd = carInfo.PriceInUsd * days;
        await repository.SaveChangesAsync();
    }
}
