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
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings()
    {
        var dtos = await bookingService.GetAllBookingsAsync();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetBookingById([FromRoute, Required] int id)
    {
        var dto = await bookingService.GetBookingByIdAsync(id);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var createdBooking = await bookingService.CreateBookingAsnyc(dto);
        return CreatedAtRoute(nameof(createdBooking), new { id = createdBooking.Id }, createdBooking);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDto>> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusDto updateStatusDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var dto = await bookingService.SetBookingStatusAsync(id, updateStatusDto.Status);
        return Ok(dto);
    }
}