using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController(IBookingService bookingService) : Controller
{
    [Authorize(Policy = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings([FromQuery, Required] PaginationDto pagination)
    {
        var dtos = await bookingService.GetAllBookingsAsync(pagination);
        return Ok(dtos);
    }

    [Authorize(Policy = "User")]
    [HttpGet("user")]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllUserBookings([FromQuery, Required] PaginationDto pagination)
    {
        var dtos = await bookingService.GetAllUserBookingsAsync(this.GetUserId(), pagination);
        return Ok(dtos);
    }

    [Authorize(Policy = "All")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetBookingById([FromRoute, Required] int id)
    {
        var dto = await bookingService.GetBookingByIdAsync(id);
        return Ok(dto);
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

    [Authorize(Policy = "All")]
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody, Required] CreateBookingDto dto)
    {
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

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
}