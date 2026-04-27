using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarService.Common;
using CarService.Exceptions;
using CarService.Mappings;
using CarService.Models.DTOs;
using CarService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CarService.Tests.Services;

public class CarServiceTests
{
    private readonly CarService.Services.CarService _carService;
    private readonly IMapper _mapper;
    private readonly Mock<ICarRepository> _mockCarRepository;
    private readonly Mock<IMessageProducer> _mockMessageProducer;

    public CarServiceTests()
    {
        _mockCarRepository = new Mock<ICarRepository>();
        _mockMessageProducer = new Mock<IMessageProducer>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), new NullLoggerFactory());
        _mapper = config.CreateMapper();

        _carService = new CarService.Services.CarService(_mockCarRepository.Object, _mapper, _mockMessageProducer.Object);
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
    public async Task GetAllCarsAsync_ShouldReturnAllCars_WhenNoFilterApplied()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Maintenance
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var result = await _carService.GetAllCarsAsync(null, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(2);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(2);
        carDtos[0].Make.Should().Be("Toyota");
        carDtos[1].Make.Should().Be("Honda");
        _mockCarRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldReturnEmptyList_WhenNoCarsExist()
    {
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Car>().AsQueryable());

        var result = await _carService.GetAllCarsAsync(null, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(0);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().BeEmpty();
        _mockCarRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldFilterByManufacturer_WhenManufacturerProvided()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Maintenance
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var filter = new CarFilterDto { CarManufacturer = "Toyota" };

        var result = await _carService.GetAllCarsAsync(filter, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(1);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(1);
        carDtos[0].Make.Should().Be("Toyota");
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldFilterByModelBranch_WhenModelProvided()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Maintenance
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var filter = new CarFilterDto { CarModel = "Civic", CarManufacturer = "Honda" };

        var result = await _carService.GetAllCarsAsync(filter, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(1);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(1);
        carDtos[0].Model.Should().Be("Civic");
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldFilterByStatus_WhenStatusProvided()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Maintenance
            },
            new()
            {
                Id = 3, Make = "Ford", Model = "Focus", Year = 2021, PriceInUsd = 21000, Status = CarStatus.Available
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var filter = new CarFilterDto { Status = CarStatus.Available };

        var result = await _carService.GetAllCarsAsync(filter, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(2);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(2);
        carDtos.Should().OnlyContain(car => car.Status == CarStatus.Available);
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldFilterByYear_WhenYearProvided()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Available
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var filter = new CarFilterDto { Year = 2023 };

        var result = await _carService.GetAllCarsAsync(filter, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(1);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(1);
        carDtos[0].Year.Should().Be(2023);
    }


    [Fact]
    public async Task GetAllCarsAsync_ShouldFilterByAvailability_WhenDatesProvided()
    {
        var overlappingBooking = new DateRange()
        {
            PickupDate = new DateTime(2024, 5, 10),
            DropOffDate = new DateTime(2024, 5, 12)
        };
        var cars = new List<Car>
         {
             new()
             {
                 Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available,
                 UnavailableDates = new HashSet<DateRange> { overlappingBooking }
             },
             new()
             {
                 Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Available,
                 UnavailableDates = new HashSet<DateRange>()
             }
         };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var filter = new CarFilterDto
        {
            PickupDate = new DateTime(2024, 5, 11),
            DropoffDate = new DateTime(2024, 5, 13)
        };

        var result = await _carService.GetAllCarsAsync(filter, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(1);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(1);
        carDtos[0].Make.Should().Be("Honda");
    }

    [Fact]
    public async Task GetAllCarsAsync_ShouldApplyPagination_AfterFiltering()
    {
        var cars = new List<Car>
        {
            new()
            {
                Id = 1, Make = "Toyota", Model = "Camry", Year = 2022, PriceInUsd = 25000, Status = CarStatus.Available
            },
            new()
            {
                Id = 2, Make = "Honda", Model = "Civic", Year = 2023, PriceInUsd = 23000, Status = CarStatus.Maintenance
            },
            new()
            {
                Id = 3, Make = "Ford", Model = "Focus", Year = 2021, PriceInUsd = 21000, Status = CarStatus.Available
            }
        };
        _mockCarRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(cars.AsQueryable());

        var result = await _carService.GetAllCarsAsync(null, new PaginationDto { Skip = 1, Take = 1 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(3);
        var carDtos = result.Elements.ToArray();
        carDtos.Should().HaveCount(1);
        carDtos[0].Id.Should().Be(2);
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
            c.PriceInUsd == 26000 &&
            c.Status == CarStatus.Available
        )), Times.Once);
        _mockMessageProducer.Verify(mp => mp.SendMaintenanceInfoMessageAsync(It.IsAny<MaintainanceStartInfo>()), Times.Never);
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

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCar);
        _mockCarRepository.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockMessageProducer.Setup(mp => mp.SendMaintenanceInfoMessageAsync(It.IsAny<MaintainanceStartInfo>()))
            .Returns(Task.CompletedTask);

        var result = await _carService.SetCarStatusAsync(1, CarStatus.Maintenance);

        result.Should().NotBeNull();
        result!.Status.Should().Be(CarStatus.Maintenance);
        existingCar.Status.Should().Be(CarStatus.Maintenance);
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCarRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        _mockMessageProducer.Verify(mp => mp.SendMaintenanceInfoMessageAsync(It.Is<MaintainanceStartInfo>(m => m.CarId == 1 && m.StartDate == DateTime.Now.Date)), Times.Once);
    }

    [Fact]
    public async Task SetCarStatusAsync_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Car?)null);

        var act = async () => await _carService.SetCarStatusAsync(999, CarStatus.Maintenance);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        _mockCarRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        _mockMessageProducer.Verify(mp => mp.SendMaintenanceInfoMessageAsync(It.IsAny<MaintainanceStartInfo>()), Times.Never);
    }

    [Theory]
    [InlineData(CarStatus.Available)]
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



        _mockCarRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingCar);
        _mockCarRepository.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockMessageProducer.Setup(mp => mp.SendMaintenanceInfoMessageAsync(It.IsAny<MaintainanceStartInfo>()))
            .Returns(Task.CompletedTask);

        var result = await _carService.SetCarStatusAsync(1, newStatus);

        result.Should().NotBeNull();
        result!.Status.Should().Be(newStatus);
        existingCar.Status.Should().Be(newStatus); // Verify the entity was modified
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockCarRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        if (newStatus == CarStatus.Maintenance)
        {
            _mockMessageProducer.Verify(mp => mp.SendMaintenanceInfoMessageAsync(It.Is<MaintainanceStartInfo>(m => m.CarId == 1 && m.StartDate == DateTime.Now.Date)), Times.Once);
        }
        else
        {
            _mockMessageProducer.Verify(mp => mp.SendMaintenanceInfoMessageAsync(It.IsAny<MaintainanceStartInfo>()), Times.Never);
        }
    }

    #endregion

    #region HandleBookingInfoAsync Tests

    [Fact]
    public async Task HandleBookingInfoAsync_ShouldSendUnavailable_WhenCarNotFound()
    {
        var bookingInfo = new BookingInfo
        {
            CarId = 10,
            BookingId = 200,
            PickupDate = new DateTime(2024, 6, 1),
            DropoffDate = new DateTime(2024, 6, 5),
            Type = BookingType.Check
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync((Car?)null);
        _mockMessageProducer.Setup(mp => mp.SendCarInfoMessageAsync(It.IsAny<CarInfo>()))
            .Returns(Task.CompletedTask);

        await _carService.HandleBookingInfoAsync(bookingInfo);

        _mockMessageProducer.Verify(mp => mp.SendCarInfoMessageAsync(It.Is<CarInfo>(info =>
            info.CarId == 10 &&
            info.BookingId == 200 &&
            info.IsAvailable == false
        )), Times.Once);
        _mockCarRepository.Verify(repo => repo.AddUnavailableDateRangeAsync(It.IsAny<Car>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _mockCarRepository.Verify(repo => repo.RemoveUnavailableDateRangeAsync(It.IsAny<Car>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task HandleBookingInfoAsync_ShouldAddUnavailableAndSendAvailableInfo_WhenCheckAndCarAvailable()
    {
        var car = new Car
        {
            Id = 5,
            Make = "Toyota",
            Model = "Corolla",
            Year = 2021,
            PriceInUsd = 20000,
            Status = CarStatus.Available
        };
        var bookingInfo = new BookingInfo
        {
            CarId = 5,
            BookingId = 300,
            PickupDate = new DateTime(2024, 7, 10),
            DropoffDate = new DateTime(2024, 7, 12),
            Type = BookingType.Check
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(5)).ReturnsAsync(car);
        _mockCarRepository.Setup(repo => repo.AddUnavailableDateRangeAsync(car, bookingInfo.BookingId, bookingInfo.PickupDate, bookingInfo.DropoffDate))
            .Returns(Task.CompletedTask);
        _mockMessageProducer.Setup(mp => mp.SendCarInfoMessageAsync(It.IsAny<CarInfo>()))
            .Returns(Task.CompletedTask);

        await _carService.HandleBookingInfoAsync(bookingInfo);

        _mockCarRepository.Verify(repo => repo.AddUnavailableDateRangeAsync(car, bookingInfo.BookingId, bookingInfo.PickupDate, bookingInfo.DropoffDate), Times.Once);
        _mockMessageProducer.Verify(mp => mp.SendCarInfoMessageAsync(It.Is<CarInfo>(info =>
            info.CarId == 5 &&
            info.BookingId == 300 &&
            info.IsAvailable == true &&
            info.Make == "Toyota" &&
            info.Model == "Corolla" &&
            info.Year == 2021 &&
            info.PriceInUsd == 20000
        )), Times.Once);
        _mockCarRepository.Verify(repo => repo.RemoveUnavailableDateRangeAsync(It.IsAny<Car>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task HandleBookingInfoAsync_ShouldSendUnavailableInfo_WhenCheckAndCarNotAvailable()
    {
        var car = new Car
        {
            Id = 6,
            Make = "Honda",
            Model = "Civic",
            Year = 2020,
            PriceInUsd = 18000,
            Status = CarStatus.Maintenance
        };
        var bookingInfo = new BookingInfo
        {
            CarId = 6,
            BookingId = 400,
            PickupDate = new DateTime(2024, 8, 1),
            DropoffDate = new DateTime(2024, 8, 3),
            Type = BookingType.Check
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(6)).ReturnsAsync(car);
        _mockMessageProducer.Setup(mp => mp.SendCarInfoMessageAsync(It.IsAny<CarInfo>()))
            .Returns(Task.CompletedTask);

        await _carService.HandleBookingInfoAsync(bookingInfo);

        _mockCarRepository.Verify(repo => repo.AddUnavailableDateRangeAsync(It.IsAny<Car>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _mockMessageProducer.Verify(mp => mp.SendCarInfoMessageAsync(It.Is<CarInfo>(info =>
            info.CarId == 6 &&
            info.BookingId == 400 &&
            info.IsAvailable == false
        )), Times.Once);
    }

    [Fact]
    public async Task HandleBookingInfoAsync_ShouldRemoveUnavailable_WhenCanceled()
    {
        var car = new Car
        {
            Id = 7,
            Make = "Ford",
            Model = "Focus",
            Year = 2019,
            PriceInUsd = 15000,
            Status = CarStatus.Available
        };
        var bookingInfo = new BookingInfo
        {
            CarId = 7,
            BookingId = 500,
            PickupDate = new DateTime(2024, 9, 5),
            DropoffDate = new DateTime(2024, 9, 9),
            Type = BookingType.Canceled
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(7)).ReturnsAsync(car);
        _mockCarRepository.Setup(repo => repo.RemoveUnavailableDateRangeAsync(car, bookingInfo.BookingId, bookingInfo.PickupDate, bookingInfo.DropoffDate))
            .Returns(Task.CompletedTask);

        await _carService.HandleBookingInfoAsync(bookingInfo);

        _mockCarRepository.Verify(repo => repo.RemoveUnavailableDateRangeAsync(car, bookingInfo.BookingId, bookingInfo.PickupDate, bookingInfo.DropoffDate), Times.Once);
        _mockMessageProducer.Verify(mp => mp.SendCarInfoMessageAsync(It.IsAny<CarInfo>()), Times.Never);
    }

    #endregion
}
