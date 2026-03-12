using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using FluentAssertions;
using Moq;

namespace CarRentalService.Tests.Services;

public class BookingServiceTests
{
    private readonly BookingService _bookingService;
    private readonly Mock<IBookingRepository> _mockBookingRepository;
    private readonly Mock<ICarRepository> _mockCarRepository;
    private readonly Mock<ICarService> _mockCarService;
    private readonly IMapper _mapper;

    public BookingServiceTests()
    {
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockCarRepository = new Mock<ICarRepository>();
        _mockCarService = new Mock<ICarService>();

        _mockBookingRepository.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Booking, BookingDto>()
                .ForMember(d => d.TotalCostInUsd, opt => opt.MapFrom(s => s.TotalCostInUsd));
            cfg.CreateMap<CreateBookingDto, Booking>();
        });
        _mapper = config.CreateMapper();

        _bookingService = new BookingService(
            _mockBookingRepository.Object,
            _mockCarRepository.Object,
            _mapper,
            _mockCarService.Object);
    }

    #region GetAllBookingsAsync Tests

    [Fact]
    public async Task GetAllBookingsAsync_ShouldReturnAllBookings_WhenBookingsExist()
    {
        var bookings = new List<Booking>
        {
            CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked),
            CreateBooking(id: 2, carId: 11, status: BookingStatus.Canceled)
        };

        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings);

        var result = await _bookingService.GetAllBookingsAsync();

        var bookingDtos = result as BookingDto[] ?? result.ToArray();
        bookingDtos.Should().HaveCount(2);
        bookingDtos[0].Id.Should().Be(1);
        bookingDtos[1].Status.Should().Be(BookingStatus.Canceled);
        _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllBookingsAsync_ShouldReturnEmptyList_WhenNoBookingsExist()
    {
        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>());

        var result = await _bookingService.GetAllBookingsAsync();

        var bookingDtos = result as BookingDto[] ?? result.ToArray();
        bookingDtos.Should().NotBeNull();
        bookingDtos.Should().BeEmpty();
        _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetBookingByIdAsync Tests

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBooking_WhenBookingExists()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.GetBookingByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.CarId.Should().Be(10);
        result.Status.Should().Be(BookingStatus.Booked);
        _mockBookingRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Booking?)null);

        var act = async () => await _bookingService.GetBookingByIdAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockBookingRepository.Verify(repo => repo.GetByIdAsync(999), Times.Once);
    }

    #endregion

    #region CreateBookingAsync Tests

    [Fact]
    public async Task CreateBookingAsnyc_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 123,
            UserId = 1,
            PickupDate = DateTime.UtcNow,
            DropoffDate = DateTime.UtcNow.AddDays(2)
        };

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(123)).ReturnsAsync((Car?)null);

        var act = async () => await _bookingService.CreateBookingAsnyc(createBookingDto);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(123), Times.Once);
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldThrowNotAllowedException_WhenCarIsNotAvailable()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            UserId = 1,
            PickupDate = DateTime.UtcNow,
            DropoffDate = DateTime.UtcNow.AddDays(2)
        };

        var car = CreateCar(id: 10, status: CarStatus.Rented);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);

        var act = async () => await _bookingService.CreateBookingAsnyc(createBookingDto);

        await act.Should().ThrowAsync<NotAllowedException>();
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldCreateAndReturnBooking_WhenCarIsAvailable()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            UserId = 1,
            PickupDate = DateTime.UtcNow,
            DropoffDate = DateTime.UtcNow.AddDays(2)
        };

        var car = CreateCar(id: 10, status: CarStatus.Available);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);

        var createdBooking = CreateBooking(id: 99, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.AddAsync(It.IsAny<Booking>())).ReturnsAsync(createdBooking);

        var result = await _bookingService.CreateBookingAsnyc(createBookingDto);

        result.Should().NotBeNull();
        result.Id.Should().Be(99);
        result.CarId.Should().Be(10);
        result.Status.Should().Be(BookingStatus.Booked);

        _mockBookingRepository.Verify(repo => repo.AddAsync(It.Is<Booking>(b => b.CarId == 10)), Times.Once);
    }

    #endregion

    #region DeleteBookingAsync Tests

    [Fact]
    public async Task DeleteBookingAsync_ShouldReturnTrue_WhenBookingIsDeleted()
    {
        _mockBookingRepository.Setup(repo => repo.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _bookingService.DeleteBookingAsync(1);

        result.Should().BeTrue();
        _mockBookingRepository.Verify(repo => repo.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteBookingAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        _mockBookingRepository.Setup(repo => repo.DeleteAsync(999)).ReturnsAsync(false);

        var act = async () => await _bookingService.DeleteBookingAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockBookingRepository.Verify(repo => repo.DeleteAsync(999), Times.Once);
    }

    #endregion

    #region SetBookingStatusAsync Tests

    [Fact]
    public async Task SetBookingStatusAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Booking?)null);

        var act = async () => await _bookingService.SetBookingStatusAsync(1, BookingStatus.Booked);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockBookingRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldThrowNotAllowedException_WhenBookingIsCompleted()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Completed);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var act = async () => await _bookingService.SetBookingStatusAsync(1, BookingStatus.Canceled);

        await act.Should().ThrowAsync<NotAllowedException>();
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetBookedAndUpdateCarStatus()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.PickedUp);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);
        _mockCarService
            .Setup(service => service.SetCarStatusAsync(10, CarStatus.Rented))
            .ReturnsAsync(new CarDto
            {
                Id = 10,
                Make = "Toyota",
                Model = "Camry",
                Year = 2022,
                PriceInUsd = 25000,
                Status = CarStatus.Rented
            });

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.Booked);

        result.Status.Should().Be(BookingStatus.Booked);
        _mockCarService.Verify(service => service.SetCarStatusAsync(10, CarStatus.Rented), Times.Once);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetCanceledAndUpdateCarStatus()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);
        _mockCarService
            .Setup(service => service.SetCarStatusAsync(10, CarStatus.Available))
            .ReturnsAsync(new CarDto
            {
                Id = 10,
                Make = "Toyota",
                Model = "Camry",
                Year = 2022,
                PriceInUsd = 25000,
                Status = CarStatus.Available
            });

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.Canceled);

        result.Status.Should().Be(BookingStatus.Canceled);
        _mockCarService.Verify(service => service.SetCarStatusAsync(10, CarStatus.Available), Times.Once);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetPickedUp_WithoutChangingCarStatus()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.PickedUp);

        result.Status.Should().Be(BookingStatus.PickedUp);
        _mockCarService.Verify(service => service.SetCarStatusAsync(It.IsAny<int>(), It.IsAny<CarStatus>()), Times.Never);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetReturnLate_WithoutChangingCarStatus()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.PickedUp);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.ReturnLate);

        result.Status.Should().Be(BookingStatus.ReturnLate);
        _mockCarService.Verify(service => service.SetCarStatusAsync(It.IsAny<int>(), It.IsAny<CarStatus>()), Times.Never);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetCompletedAndUpdateCarStatus()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.ReturnLate);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);
        _mockCarService
            .Setup(service => service.SetCarStatusAsync(10, CarStatus.Available))
            .ReturnsAsync(new CarDto
            {
                Id = 10,
                Make = "Toyota",
                Model = "Camry",
                Year = 2022,
                PriceInUsd = 25000,
                Status = CarStatus.Available
            });

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.Completed);

        result.Status.Should().Be(BookingStatus.Completed);
        _mockCarService.Verify(service => service.SetCarStatusAsync(10, CarStatus.Available), Times.Once);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    #endregion

    private static Booking CreateBooking(int id, int carId, BookingStatus status)
    {
        var car = CreateCar(carId, CarStatus.Available);
        return new Booking
        {
            Id = id,
            CarId = carId,
            Car = car,
            BookingDate = DateTime.UtcNow.AddDays(-1),
            PickupDate = DateTime.UtcNow,
            DropoffDate = DateTime.UtcNow.AddDays(2),
            TotalCostInUsd = 150,
            Status = status
        };
    }

    private static Car CreateCar(int id, CarStatus status)
    {
        return new Car
        {
            Id = id,
            Make = "Toyota",
            Model = "Camry",
            Year = 2022,
            PriceInUsd = 25000,
            Status = status
        };
    }
}
