using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalService.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController(ICarService carService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CarDto>>> GetAllCars()
    {
        var cars = await carService.GetAllCarsAsync();
        return Ok(cars);
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> GetCarById(int id)
    {
        var car = await carService.GetCarByIdAsync(id);
        return Ok(car);
    }


    [HttpPost]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> CreateCar([FromBody] CreateCarDto createCarDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var createdCar = await carService.CreateCarAsync(createCarDto);
        return CreatedAtAction(nameof(GetCarById), new { id = createdCar.Id }, createdCar);
    }


    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> UpdateCar(int id, [FromBody] UpdateCarDto updateCarDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var updatedCar = await carService.UpdateCarAsync(id, updateCarDto);
        return Ok(updatedCar);
    }


    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCar(int id)
    {
        await carService.DeleteCarAsync(id);
        return NoContent();
    }


    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> UpdateCarStatus(int id, [FromBody] UpdateCarStatusDto updateStatusDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var car = await carService.SetCarStatusAsync(id, updateStatusDto.Status);
        return Ok(car);
    }
}