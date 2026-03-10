using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarRentalService.Exceptions;
using CarRentalService.Mappings;
using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using FluentAssertions;
using Moq;

namespace CarRentalService.Tests.Services;

public class CarServiceTests
{
    private readonly CarService _carService;
    private readonly IMapper _mapper;
    private readonly Mock<ICarRepository> _mockCarRepository;

    public CarServiceTests()
    {
        _mockCarRepository = new Mock<ICarRepository>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _carService = new CarService(_mockCarRepository.Object, _mapper);
    }

    #region CreateCarAsync Tests

    [Fact]
    public async Task CreateCarAsync_ShouldCreateAndReturnCar_WithValidData()
    {
        var createCarDto = new CreateCarDto
        {
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000
        };

        var createdCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };

        _mockCarRepository.Setup(repo => repo.AddAsync(It.IsAny<Car>())).ReturnsAsync(createdCar);

        var result = await _carService.CreateCarAsync(createCarDto);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Camry");
        result.Year.Should().Be(2022);
        result.PriceInUsd.Should().Be(25000);
        result.Status.Should().Be(CarStatus.Available);

        _mockCarRepository.Verify(repo => repo.AddAsync(It.Is<Car>(c =>
            c.Make == "Toyota" &&
            c.Model == "Camry" &&
            c.Year == 2022 &&
            c.PriceInUsd == 25000
        )), Times.Once);
    }

    #endregion

    #region GetAllCarsAsync Tests

    [Fact]
    public async Task GetAllCarsAsync_ShouldReturnAllCars_WhenCarsExist()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Rented
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars);

        var result = await _carService.GetAllCarsAsync();

        var enumerable = result as CarDto[] ?? result.ToArray();
        enumerable.Should().NotBeNull();
        enumerable.Should().HaveCount(2);
        var carDtos = enumerable.ToList();
        carDtos[0].Make.Should().Be("Toyota");
        carDtos[1].Make.Should().Be("Honda");
        _mockCarRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldReturnEmptyList_WhenNoCarsExist()
    {
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Car>());

        var result = await _carService.GetAllCarsAsync();

        var carDtos = result as CarDto[] ?? result.ToArray();
        carDtos.Should().NotBeNull();
        carDtos.Should().BeEmpty();
        _mockCarRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetCarByIdAsync Tests

    [Fact]
    public async Task GetCarByIdAsync_ShouldReturnCar_WhenCarExists()
    {
        var car = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(car);

        var result = await _carService.GetCarByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Camry");
        result.Year.Should().Be(2022);
        result.PriceInUsd.Should().Be(25000);
        result.Status.Should().Be(CarStatus.Available);
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetCarByIdAsync_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Car?)null);

        var act = async () => await _carService.GetCarByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
    }

    #endregion

    #region UpdateCarAsync Tests

    [Fact]
    public async Task UpdateCarAsync_ShouldUpdateAndReturnCar_WhenCarExists()
    {
        var updateCarDto = new UpdateCarDto
        {
            Make = "Toyota",
            Model = "Camry Updated",
            Year = 2023,
            PriceInUsd = 26000
        };

        var existingCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };

        var updatedCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry Updated",
            Year = 2023,
            PriceInUsd = 26000,
            Status = CarStatus.Available
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCar);
        _mockCarRepository.Setup(repo => repo.UpdateAsync(It.IsAny<Car>())).ReturnsAsync(updatedCar);

        var result = await _carService.UpdateCarAsync(1, updateCarDto);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Camry Updated");
        result.Year.Should().Be(2023);
        result.PriceInUsd.Should().Be(26000);

        _mockCarRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCarRepository.Verify(repo => repo.UpdateAsync(It.Is<Car>(c =>
            c.Id == 1 &&
            c.Make == "Toyota" &&
            c.Model == "Camry Updated" &&
            c.Year == 2023 &&
            c.PriceInUsd == 26000
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateCarAsync_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        var updateCarDto = new UpdateCarDto
        {
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Car?)null);

        var act = async () => await _carService.UpdateCarAsync(999, updateCarDto);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
    }

    #endregion

    #region DeleteCarAsync Tests

    [Fact]
    public async Task DeleteCarAsync_ShouldReturnTrue_WhenCarIsDeleted()
    {
        _mockCarRepository.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _carService.DeleteCarAsync(1);

        result.Should().BeTrue();
        _mockCarRepository.Verify(repo => repo.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteCarAsync_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        _mockCarRepository.Setup(repo => repo.DeleteAsync(999)).ReturnsAsync(false);

        var act = async () => await _carService.DeleteCarAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.DeleteAsync(999), Times.Once);
    }

    #endregion

    #region SetCarStatusAsync Tests

    [Fact]
    public async Task SetCarStatusAsync_ShouldUpdateStatus_WhenCarExists()
    {
        var existingCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };

        var updatedCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Rented
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCar);
        _mockCarRepository.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _carService.SetCarStatusAsync(1, CarStatus.Rented);

        result.Should().NotBeNull();
        result!.Status.Should().Be(CarStatus.Rented);
        existingCar.Status.Should().Be(CarStatus.Rented);
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCarRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetCarStatusAsync_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Car?)null);

        var act = async () => await _carService.SetCarStatusAsync(999, CarStatus.Rented);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockCarRepository.Verify(repo => repo.UpdateAsync(It.IsAny<Car>()), Times.Never);
    }

    [Theory]
    [InlineData(CarStatus.Available)]
    [InlineData(CarStatus.Rented)]
    [InlineData(CarStatus.Maintenance)]
    public async Task SetCarStatusAsync_ShouldHandleAllStatuses_WhenCarExists(CarStatus newStatus)
    {
        var existingCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = CarStatus.Available
        };

        var updatedCar = new Car
        {
            Id = 1,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = newStatus
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCar);
        _mockCarRepository.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _carService.SetCarStatusAsync(1, newStatus);

        result.Should().NotBeNull();
        result!.Status.Should().Be(newStatus);
        existingCar.Status.Should().Be(newStatus); // Verify the entity was modified
    }

    #endregion
}