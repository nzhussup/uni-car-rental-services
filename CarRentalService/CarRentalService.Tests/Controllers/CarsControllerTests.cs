using CarRentalService.Controllers;
using CarRentalService.Models;
using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRentalService.Tests.Controllers;

public class CarsControllerTests
{
    private readonly Mock<ICarService> _mockCarService;
    private readonly CarsController _controller;

    public CarsControllerTests()
    {
        _mockCarService = new Mock<ICarService>();
        _controller = new CarsController(_mockCarService.Object);
    }

    #region GetAllCars Tests

    [Fact]
    public async Task GetAllCars_ShouldReturnOkWithCars_WhenCarsExist()
    {
        var cars = new List<CarDto>
        {
            new() { Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available },
            new() { Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Rented }
        };
        _mockCarService.Setup(service => service.GetAllCarsAsync()).ReturnsAsync(cars);

        var result = await _controller.GetAllCars();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCars = okResult.Value.Should().BeAssignableTo<IEnumerable<CarDto>>().Subject;
        returnedCars.Should().HaveCount(2);
        _mockCarService.Verify(service => service.GetAllCarsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllCars_ShouldReturnOkWithEmptyList_WhenNoCarsExist()
    {
        _mockCarService.Setup(service => service.GetAllCarsAsync()).ReturnsAsync(new List<CarDto>());

        var result = await _controller.GetAllCars();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCars = okResult.Value.Should().BeAssignableTo<IEnumerable<CarDto>>().Subject;
        returnedCars.Should().BeEmpty();
    }

    #endregion

    #region GetCarById Tests

    [Fact]
    public async Task GetCarById_ShouldReturnOkWithCar_WhenCarExists()
    {
        var carDto = new CarDto
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };
        _mockCarService.Setup(service => service.GetCarByIdAsync(1)).ReturnsAsync(carDto);

        var result = await _controller.GetCarById(1);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCar = okResult.Value.Should().BeOfType<CarDto>().Subject;
        returnedCar.Id.Should().Be(1);
        returnedCar.Make.Should().Be("Toyota");
        _mockCarService.Verify(service => service.GetCarByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetCarById_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        _mockCarService.Setup(service => service.GetCarByIdAsync(999)).ReturnsAsync((CarDto?)null);

        var result = await _controller.GetCarById(999);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Car with ID 999 not found");
        _mockCarService.Verify(service => service.GetCarByIdAsync(999), Times.Once);
    }

    #endregion

    #region CreateCar Tests

    [Fact]
    public async Task CreateCar_ShouldReturnCreatedAtAction_WhenCarIsCreated()
    {
        var createCarDto = new CreateCarDto
        {
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000
        };

        var createdCarDto = new CarDto
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };

        _mockCarService.Setup(service => service.CreateCarAsync(createCarDto)).ReturnsAsync(createdCarDto);

        var result = await _controller.CreateCar(createCarDto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(CarsController.GetCarById));
        createdResult.RouteValues!["id"].Should().Be(1);
        var returnedCar = createdResult.Value.Should().BeOfType<CarDto>().Subject;
        returnedCar.Id.Should().Be(1);
        returnedCar.Make.Should().Be("Toyota");
        _mockCarService.Verify(service => service.CreateCarAsync(createCarDto), Times.Once);
    }

    [Fact]
    public async Task CreateCar_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        var createCarDto = new CreateCarDto
        {
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000
        };
        _controller.ModelState.AddModelError("Make", "Make is required");

        var result = await _controller.CreateCar(createCarDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _mockCarService.Verify(service => service.CreateCarAsync(It.IsAny<CreateCarDto>()), Times.Never);
    }

    #endregion

    #region UpdateCar Tests

    [Fact]
    public async Task UpdateCar_ShouldReturnOkWithUpdatedCar_WhenCarExists()
    {
        var updateCarDto = new UpdateCarDto
        {
            Make = "Toyota",
            Model = "Camry Updated",
            Year = 2023,
            PriceInUsd = 26000
        };

        var updatedCarDto = new CarDto
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry Updated",
            Year = 2023,
            PriceInUsd = 26000,
            Status = CarStatus.Available
        };

        _mockCarService.Setup(service => service.UpdateCarAsync(1, updateCarDto)).ReturnsAsync(updatedCarDto);

        var result = await _controller.UpdateCar(1, updateCarDto);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCar = okResult.Value.Should().BeOfType<CarDto>().Subject;
        returnedCar.Id.Should().Be(1);
        returnedCar.Model.Should().Be("Camry Updated");
        returnedCar.Year.Should().Be(2023);
        _mockCarService.Verify(service => service.UpdateCarAsync(1, updateCarDto), Times.Once);
    }

    [Fact]
    public async Task UpdateCar_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        var updateCarDto = new UpdateCarDto
        {
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000
        };

        _mockCarService.Setup(service => service.UpdateCarAsync(999, updateCarDto)).ReturnsAsync((CarDto?)null);

        var result = await _controller.UpdateCar(999, updateCarDto);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Car with ID 999 not found");
        _mockCarService.Verify(service => service.UpdateCarAsync(999, updateCarDto), Times.Once);
    }

    [Fact]
    public async Task UpdateCar_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        var updateCarDto = new UpdateCarDto
        {
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000
        };
        _controller.ModelState.AddModelError("Make", "Make is required");

        var result = await _controller.UpdateCar(1, updateCarDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _mockCarService.Verify(service => service.UpdateCarAsync(It.IsAny<int>(), It.IsAny<UpdateCarDto>()), Times.Never);
    }

    #endregion

    #region DeleteCar Tests

    [Fact]
    public async Task DeleteCar_ShouldReturnNoContent_WhenCarIsDeleted()
    {
        _mockCarService.Setup(service => service.DeleteCarAsync(1)).ReturnsAsync(true);

        var result = await _controller.DeleteCar(1);

        result.Should().BeOfType<NoContentResult>();
        _mockCarService.Verify(service => service.DeleteCarAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteCar_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        _mockCarService.Setup(service => service.DeleteCarAsync(999)).ReturnsAsync(false);

        var result = await _controller.DeleteCar(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Car with ID 999 not found");
        _mockCarService.Verify(service => service.DeleteCarAsync(999), Times.Once);
    }

    #endregion

    #region UpdateCarStatus Tests

    [Fact]
    public async Task UpdateCarStatus_ShouldReturnOkWithUpdatedCar_WhenStatusIsUpdated()
    {
        var updateStatusDto = new UpdateCarStatusDto { Status = CarStatus.Rented };
        var updatedCarDto = new CarDto
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Rented
        };

        _mockCarService.Setup(service => service.SetCarStatusAsync(1, CarStatus.Rented)).ReturnsAsync(updatedCarDto);

        var result = await _controller.UpdateCarStatus(1, updateStatusDto);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCar = okResult.Value.Should().BeOfType<CarDto>().Subject;
        returnedCar.Id.Should().Be(1);
        returnedCar.Status.Should().Be(CarStatus.Rented);
        _mockCarService.Verify(service => service.SetCarStatusAsync(1, CarStatus.Rented), Times.Once);
    }

    [Fact]
    public async Task UpdateCarStatus_ShouldReturnNotFound_WhenCarDoesNotExist()
    {
        var updateStatusDto = new UpdateCarStatusDto { Status = CarStatus.Rented };
        _mockCarService.Setup(service => service.SetCarStatusAsync(999, CarStatus.Rented)).ReturnsAsync((CarDto?)null);

        var result = await _controller.UpdateCarStatus(999, updateStatusDto);

        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be("Car with ID 999 not found");
        _mockCarService.Verify(service => service.SetCarStatusAsync(999, CarStatus.Rented), Times.Once);
    }

    [Fact]
    public async Task UpdateCarStatus_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        var updateStatusDto = new UpdateCarStatusDto { Status = CarStatus.Rented };
        _controller.ModelState.AddModelError("Status", "Status is required");

        var result = await _controller.UpdateCarStatus(1, updateStatusDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _mockCarService.Verify(service => service.SetCarStatusAsync(It.IsAny<int>(), It.IsAny<CarStatus>()), Times.Never);
    }

    [Theory]
    [InlineData(CarStatus.Available)]
    [InlineData(CarStatus.Rented)]
    [InlineData(CarStatus.Maintenance)]
    public async Task UpdateCarStatus_ShouldHandleAllStatuses_WhenCarExists(CarStatus status)
    {

        var updateStatusDto = new UpdateCarStatusDto { Status = status };
        var updatedCarDto = new CarDto
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = status
        };

        _mockCarService.Setup(service => service.SetCarStatusAsync(1, status)).ReturnsAsync(updatedCarDto);

        var result = await _controller.UpdateCarStatus(1, updateStatusDto);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCar = okResult.Value.Should().BeOfType<CarDto>().Subject;
        returnedCar.Status.Should().Be(status);
    }

    #endregion
}
