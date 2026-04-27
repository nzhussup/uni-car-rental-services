using BookingService.Common;

namespace BookingService.Services;

public interface IMessageProducer
{
    Task SendBookingInfoAsync(BookingInfo bookingInfo);
}