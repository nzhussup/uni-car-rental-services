using CarRentalService.Data.Entities;
using CarRentalService.Models.DTOs;

namespace CarRentalService.Services;

public interface IBookingService
{
    Task<QueryResponse<BookingDto>> GetAllBookingsAsync(PaginationDto pagination);
    Task<QueryResponse<BookingDto>> GetAllUserBookingsAsync(Guid userId, PaginationDto pagination);
    Task<BookingDto> GetBookingByIdAsync(int id);
    Task<BookingDto> GetBookingByIdAsync(Guid userId, int id);
    Task<BookingDto> CreateBookingAsnyc(Guid userId, CreateBookingDto createBookingDto);
    // Do we need an UpdateBooking, since we only Touch the Status and maybe the Dropodd Date
    Task<bool> DeleteBookingAsync(int id);
    Task<BookingDto> SetBookingStatusAsync(int id, BookingStatus bookingStatus);
    Task<BookingDto> CancelBookingAsync(Guid userId, int id);
}