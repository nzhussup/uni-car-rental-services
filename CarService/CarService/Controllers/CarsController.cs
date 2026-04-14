using System.ComponentModel.DataAnnotations;
using AutoMapper;
using CarService.Models.DTOs;
using CarService.Models.Responses;
using CarService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarService.Controllers;

[ApiController]
[Route("api/cars")]
public class CarsController(ICarService carService, IExtCurrencyConvertService currencyConvertService, IMapper mapper) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(QueryResponse<CarResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<CarDto>>> GetAllCars([FromQuery] CarFilterDto filter, [FromQuery, Required] PaginationDto pagination, [FromQuery] string targetCurrency = "USD")
    {
        var cars = await carService.GetAllCarsAsync(filter, pagination);
        var mappedCars = mapper.Map<QueryResponse<CarResponse>>(cars);

        var conversionTasks = mappedCars.Elements
            .Select(async car =>
            {
                car.Price = await currencyConvertService.ConvertMoney(car.Price.Amount, car.Price.Currency, targetCurrency);
            });
        await Task.WhenAll(conversionTasks);

        return Ok(mappedCars);
    }


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CarDto>> GetCarById(int id, [FromQuery] string targetCurrency = "USD")
    {
        var car = await carService.GetCarByIdAsync(id);
        var mappedCar = mapper.Map<CarDto, CarResponse>(car);
        mappedCar.Price = await currencyConvertService.ConvertMoney(mappedCar.Price.Amount, mappedCar.Price.Currency, targetCurrency);
        return Ok(mappedCar);
    }


    [Authorize(Policy = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CarDto>> CreateCar([FromBody] CreateCarDto createCarDto)
    {
        var createdCar = await carService.CreateCarAsync(createCarDto);
        return CreatedAtAction(nameof(GetCarById), new { id = createdCar.Id }, createdCar);
    }


    [Authorize(Policy = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> UpdateCar(int id, [FromBody] UpdateCarDto updateCarDto)
    {
        var updatedCar = await carService.UpdateCarAsync(id, updateCarDto);
        return Ok(updatedCar);
    }


    [Authorize(Policy = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCar(int id)
    {
        await carService.DeleteCarAsync(id);
        return NoContent();
    }


    [Authorize(Policy = "Admin")]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CarDto>> UpdateCarStatus(int id, [FromBody] UpdateCarStatusDto updateStatusDto)
    {
        var car = await carService.SetCarStatusAsync(id, updateStatusDto.Status);
        return Ok(car);
    }
}
