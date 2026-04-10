using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AutoMapper;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;
using CarRentalService.Models.Responses;
using CarRentalService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalService.Controllers;

[ApiController]
[Route("api/booking")]
public class BookingController(IBookingService bookingService, IExtCurrencyConvertService currencyConvertService, IMapper mapper) : Controller
{
    [Authorize(Policy = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(QueryResponse<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings([FromQuery, Required] PaginationDto pagination, [FromQuery] string targetCurrency = "USD")
    {
        var dtos = await bookingService.GetAllBookingsAsync(pagination);
        var mappedBookings = await ConvertPriceForBooking(targetCurrency, dtos);
        return Ok(mappedBookings);
    }

    [Authorize(Policy = "User")]
    [HttpGet("user")]
    [ProducesResponseType(typeof(QueryResponse<BookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllUserBookings([FromQuery, Required] PaginationDto pagination, [FromQuery] string targetCurrency = "USD")
    {
        var dtos = await bookingService.GetAllUserBookingsAsync(this.GetUserId(), pagination);
        var mappedBookings = await ConvertPriceForBooking(targetCurrency, dtos);
        var currentUser = GetCurrentUserFromClaims();
        foreach (var booking in mappedBookings.Elements.Where(x => x.User is null))
        {
            booking.User = currentUser;
        }
        return Ok(mappedBookings);
    }

    [Authorize(Policy = "User")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetBookingById([FromRoute, Required] int id, [FromQuery] string targetCurrency = "USD")
    {
        var dto = IsAdmin()
            ? await bookingService.GetBookingByIdAsync(id)
            : await bookingService.GetBookingByIdAsync(this.GetUserId(), id);
        var mappedBooking = mapper.Map<BookingDto, BookingResponse>(dto);
        if (mappedBooking.TotalCost is not null)
        {
            mappedBooking.TotalCost = await currencyConvertService.ConvertMoney(mappedBooking.TotalCost.Amount, mappedBooking.TotalCost.Currency, targetCurrency);
        }
        if (!IsAdmin() && mappedBooking.User is null)
        {
            mappedBooking.User = GetCurrentUserFromClaims();
        }
        return Ok(mappedBooking);
    }

    [Authorize(Policy = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> DeleteBookingById([FromRoute, Required] int id)
    {
        await bookingService.DeleteBookingAsync(id);
        return NoContent();
    }

    [Authorize(Policy = "User")]
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody, Required] CreateBookingDto dto)
    {
        if (dto is null)
        {
            return BadRequest("Request body is required.");
        }

        var createdBooking = await bookingService.CreateBookingAsnyc(this.GetUserId(), dto);
        return CreatedAtAction(nameof(GetBookingById), new { id = createdBooking.Id }, createdBooking);
    }

    [Authorize(Policy = "Admin")]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusDto updateStatusDto)
    {
        var dto = await bookingService.SetBookingStatusAsync(id, updateStatusDto.Status);
        return Ok(dto);
    }

    [Authorize(Policy = "User")]
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> CancelBookingStatus(int id)
    {
        var dto = await bookingService.CancelBookingAsync(this.GetUserId(), id);
        return Ok(dto);
    }


    private async Task<QueryResponse<BookingResponse>> ConvertPriceForBooking(string targetCurrency, QueryResponse<BookingDto> dtos)
    {
        var mappedBookings = mapper.Map<QueryResponse<BookingDto>, QueryResponse<BookingResponse>>(dtos);
        var conversionTasks = mappedBookings.Elements
            .Select(async bookingsElement =>
            {
                var totalCost = bookingsElement.TotalCost;
                if (totalCost is null)
                {
                    return;
                }

                bookingsElement.TotalCost = await currencyConvertService.ConvertMoney(
                    totalCost.Amount,
                    totalCost.Currency,
                    targetCurrency);
            });
        await Task.WhenAll(conversionTasks);

        return mappedBookings;
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null)
        {
            throw new UserIdNotFoundException("User Id not found");
        }

        if (!Guid.TryParse(userId, out var result))
        {
            throw new UserIdNotFoundException("User Id not found");
        }

        return result;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("app-admin")
               || User.Claims.Any(c =>
                   (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
                   && c.Value == "app-admin");
    }

    private UserDto GetCurrentUserFromClaims()
    {
        var id = GetUserId();
        var firstName = User.FindFirstValue("given_name")
                        ?? User.FindFirstValue(ClaimTypes.GivenName)
                        ?? User.FindFirstValue("preferred_username")
                        ?? "Unknown";
        var lastName = User.FindFirstValue("family_name")
                       ?? User.FindFirstValue(ClaimTypes.Surname)
                       ?? string.Empty;
        var email = User.FindFirstValue("email")
                    ?? User.FindFirstValue(ClaimTypes.Email)
                    ?? string.Empty;

        return new UserDto
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email
        };
    }
}
