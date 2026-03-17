using System.ComponentModel.DataAnnotations;
using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController(IBookingService bookingService) : Controller
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings([FromQuery, Required] PaginationDto pagination)
    {
        var dtos = await bookingService.GetAllBookingsAsync(pagination);
        return Ok(dtos);
    }

    [HttpGet("user")]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllUserBookings([FromQuery, Required] PaginationDto pagination)
    {
        //TODO: Add correct user id
        var dtos = await bookingService.GetAllUserBookingsAsync(0, pagination);
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetBookingById([FromRoute, Required] int id)
    {
        var dto = await bookingService.GetBookingByIdAsync(id);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> DeleteBookingById([FromRoute, Required] int id)
    {
        await bookingService.DeleteBookingAsync(id);
        return NoContent();
    }

    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> CreateBooking([FromBody, Required] CreateBookingDto dto)
    {
        var createdBooking = await bookingService.CreateBookingAsnyc(dto);
        return CreatedAtAction(nameof(GetBookingById), new { id = createdBooking.Id }, createdBooking);
    }

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

    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> CancelBookingStatus(int id)
    {
        //TODO: Add UserId
        var dto = await bookingService.CancelBookingAsync(0, id);
        return Ok(dto);
    }
}