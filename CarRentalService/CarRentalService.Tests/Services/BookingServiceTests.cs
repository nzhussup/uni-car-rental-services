using AutoMapper;
using CarRentalService.Data.Entities;
using CarRentalService.Data.Repositories;
using CarRentalService.Exceptions;
using CarRentalService.Models.DTOs;
using CarRentalService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CarRentalService.Tests.Services;

public class BookingServiceTests
{
    private static readonly DateTime TestBaseDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly BookingService _bookingService;
    private readonly Mock<IBookingRepository> _mockBookingRepository;
    private readonly Mock<ICarRepository> _mockCarRepository;
    private readonly Mock<IUserService> _mockUserService;
    private readonly IMapper _mapper;

    public BookingServiceTests()
    {
        _mockBookingRepository = new Mock<IBookingRepository>();
        _mockCarRepository = new Mock<ICarRepository>();
        _mockUserService = new Mock<IUserService>();

        _mockBookingRepository.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);
        _mockUserService
            .Setup(service => service.GetUserByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new UserDto
            {
                Id = id,
                FirstName = "Test",
                LastName = "User",
                Email = "test.user@example.com"
            });

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Booking, BookingDto>()
                .ForMember(d => d.TotalCostInUsd, opt => opt.MapFrom(s => s.TotalCostInUsd));
            cfg.CreateMap<CreateBookingDto, Booking>();
        }, new NullLoggerFactory());
        _mapper = config.CreateMapper();

        _bookingService = new BookingService(
            _mockBookingRepository.Object,
            _mockCarRepository.Object,
            _mapper,
            _mockUserService.Object);
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

        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings.AsQueryable());

        var result = await _bookingService.GetAllBookingsAsync(new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(2);
        var bookingDtos = result.Elements.ToArray();
        bookingDtos.Should().HaveCount(2);
        bookingDtos[0].Id.Should().Be(2);
        bookingDtos[1].Id.Should().Be(1);
        _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllBookingsAsync_ShouldReturnEmptyList_WhenNoBookingsExist()
    {
        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>().AsQueryable());

        var result = await _bookingService.GetAllBookingsAsync(new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(0);
        var bookingDtos = result.Elements.ToArray();
        bookingDtos.Should().BeEmpty();
        _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetAllUserBookingsAsync Tests

    [Fact]
    public async Task GetAllUserBookingsAsync_ShouldReturnUserBookings_WhenBookingsExist()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var bookings = new List<Booking>
        {
            CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: userId),
            CreateBooking(id: 2, carId: 11, status: BookingStatus.Canceled, userId: otherUserId),
            CreateBooking(id: 3, carId: 12, status: BookingStatus.PickedUp, userId: userId)
        };

        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings.AsQueryable());

        var result = await _bookingService.GetAllUserBookingsAsync(userId, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(2);
        var bookingDtos = result.Elements.ToArray();
        bookingDtos.Should().HaveCount(2);
        bookingDtos.Select(b => b.Id).Should().Equal(3, 1);
        _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUserBookingsAsync_ShouldReturnEmptyList_WhenNoBookingsForUser()
    {
        var userId = Guid.NewGuid();
        var bookings = new List<Booking>
        {
            CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: Guid.NewGuid()),
            CreateBooking(id: 2, carId: 11, status: BookingStatus.Canceled, userId: Guid.NewGuid())
        };

        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings.AsQueryable());

        var result = await _bookingService.GetAllUserBookingsAsync(userId, new PaginationDto { Skip = 0, Take = 50 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(0);
        var bookingDtos = result.Elements.ToArray();
        bookingDtos.Should().BeEmpty();
        _mockBookingRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUserBookingsAsync_ShouldApplyPagination_AfterFiltering()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var bookings = new List<Booking>
        {
            CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: userId),
            CreateBooking(id: 2, carId: 11, status: BookingStatus.Canceled, userId: userId),
            CreateBooking(id: 3, carId: 12, status: BookingStatus.PickedUp, userId: userId),
            CreateBooking(id: 4, carId: 13, status: BookingStatus.Booked, userId: otherUserId)
        };

        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings.AsQueryable());

        var result = await _bookingService.GetAllUserBookingsAsync(userId, new PaginationDto { Skip = 1, Take = 1 });

        result.Should().NotBeNull();
        result.TotalElements.Should().Be(3);
        var bookingDtos = result.Elements.ToArray();
        bookingDtos.Should().HaveCount(1);
        bookingDtos[0].Id.Should().Be(2);
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

    [Fact]
    public async Task GetBookingByIdAsync_ShouldReturnBookingForUser_WhenBookingExists()
    {
        var userId = Guid.NewGuid();
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: userId);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.GetBookingByIdAsync(userId, 1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.CarId.Should().Be(10);
        result.Status.Should().Be(BookingStatus.Booked);
        _mockBookingRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockUserService.Verify(service => service.GetUserByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotAllowedException_WhenBookingDoesNotBelongToUser()
    {
        var userId = Guid.NewGuid();
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: Guid.NewGuid());
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var act = async () => await _bookingService.GetBookingByIdAsync(userId, 1);

        await act.Should().ThrowAsync<NotAllowedException>();
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingIsNull_ForUser()
    {
        var userId = Guid.NewGuid();
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Booking?)null);

        var act = async () => await _bookingService.GetBookingByIdAsync(userId, 1);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockUserService.Verify(service => service.GetUserByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ShouldThrowNotFoundException_WhenBookingDoesNotExist_ForUser()
    {
        var userId = Guid.NewGuid();
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((Booking?)null);

        var act = async () => await _bookingService.GetBookingByIdAsync(userId, 999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region CreateBookingAsync Tests

    [Fact]
    public async Task CreateBookingAsnyc_ShouldThrowArgumentNullException_WhenDtoIsNull()
    {
        var userId = Guid.NewGuid();

        var act = async () => await _bookingService.CreateBookingAsnyc(userId, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldThrowNotFoundException_WhenCarDoesNotExist()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 123,
            PickupDate = TestBaseDate.AddDays(1),
            DropoffDate = TestBaseDate.AddDays(3)
        };
        var userId = Guid.NewGuid();

        _mockCarRepository.Setup(repo => repo.GetByIdAsync(123)).ReturnsAsync((Car?)null);

        var act = async () => await _bookingService.CreateBookingAsnyc(userId, createBookingDto);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockCarRepository.Verify(repo => repo.GetByIdAsync(123), Times.Once);
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldThrowNotAllowedException_WhenCarIsInMaintenance()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            PickupDate = TestBaseDate.AddDays(1),
            DropoffDate = TestBaseDate.AddDays(3)
        };
        var userId = Guid.NewGuid();

        var car = CreateCar(id: 10, status: CarStatus.Maintenance);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);

        var act = async () => await _bookingService.CreateBookingAsnyc(userId, createBookingDto);

        await act.Should().ThrowAsync<NotAllowedException>();
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldAllowBooking_WhenCarStatusIsAvailable_AndDatesDoNotOverlap()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            PickupDate = TestBaseDate.AddDays(7),
            DropoffDate = TestBaseDate.AddDays(9)
        };
        var userId = Guid.NewGuid();

        var car = CreateCar(id: 10, status: CarStatus.Available);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);
        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>().AsQueryable());
        _mockBookingRepository.Setup(repo => repo.AddAsync(It.IsAny<Booking>())).ReturnsAsync(
            CreateBooking(id: 99, carId: 10, status: BookingStatus.Booked, userId: userId));

        var result = await _bookingService.CreateBookingAsnyc(userId, createBookingDto);

        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Booked);
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldIgnoreCanceledAndCompletedBookings_ForOverlapCheck()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            PickupDate = new DateTime(2026, 4, 10),
            DropoffDate = new DateTime(2026, 4, 12)
        };
        var userId = Guid.NewGuid();

        var car = CreateCar(id: 10, status: CarStatus.Available);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);

        var bookings = new List<Booking>
        {
            CreateBooking(id: 1, carId: 10, status: BookingStatus.Canceled),
            CreateBooking(id: 2, carId: 10, status: BookingStatus.Completed),
        };
        bookings[0].PickupDate = new DateTime(2026, 4, 9);
        bookings[0].DropoffDate = new DateTime(2026, 4, 11);
        bookings[1].PickupDate = new DateTime(2026, 4, 11);
        bookings[1].DropoffDate = new DateTime(2026, 4, 13);
        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(bookings.AsQueryable());
        _mockBookingRepository.Setup(repo => repo.AddAsync(It.IsAny<Booking>())).ReturnsAsync(
            CreateBooking(id: 99, carId: 10, status: BookingStatus.Booked, userId: userId));

        var result = await _bookingService.CreateBookingAsnyc(userId, createBookingDto);

        result.Should().NotBeNull();
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldThrowNotAllowedException_WhenOverlappingActiveBookingExists()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            PickupDate = TestBaseDate.AddDays(10),
            DropoffDate = TestBaseDate.AddDays(12)
        };
        var userId = Guid.NewGuid();

        var car = CreateCar(id: 10, status: CarStatus.Available);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);

        var activeOverlappingBooking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked);
        activeOverlappingBooking.PickupDate = TestBaseDate.AddDays(11);
        activeOverlappingBooking.DropoffDate = TestBaseDate.AddDays(13);

        _mockBookingRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Booking> { activeOverlappingBooking }.AsQueryable());

        var act = async () => await _bookingService.CreateBookingAsnyc(userId, createBookingDto);

        await act.Should().ThrowAsync<NotAllowedException>()
            .WithMessage("*already booked for the selected dates*");
        _mockBookingRepository.Verify(repo => repo.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsnyc_ShouldCreateAndReturnBooking_WhenCarIsAvailable()
    {
        var createBookingDto = new CreateBookingDto
        {
            CarId = 10,
            PickupDate = TestBaseDate.AddDays(1),
            DropoffDate = TestBaseDate.AddDays(3)
        };
        var userId = Guid.NewGuid();

        var car = CreateCar(id: 10, status: CarStatus.Available);
        _mockCarRepository.Setup(repo => repo.GetByIdAsync(10)).ReturnsAsync(car);
        _mockBookingRepository.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Booking>().AsQueryable());

        var createdBooking = CreateBooking(id: 99, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.AddAsync(It.IsAny<Booking>())).ReturnsAsync(createdBooking);

        var result = await _bookingService.CreateBookingAsnyc(userId, createBookingDto);

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

    #region CancelBooking Tests

    [Fact]
    public async Task CancelBooking_ShouldThrowNotFoundException_WhenBookingDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync((Booking?)null);

        var act = async () => await _bookingService.CancelBookingAsync(userId, 1);

        await act.Should().ThrowAsync<NotFoundException>();
        _mockBookingRepository.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelBooking_ShouldThrowNotAllowedException_WhenBookingDoesNotBelongToUser()
    {
        var userId = Guid.NewGuid();
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: Guid.NewGuid());
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var act = async () => await _bookingService.CancelBookingAsync(userId, 1);

        await act.Should().ThrowAsync<NotAllowedException>();
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelBooking_ShouldThrowNotAllowedException_WhenBookingIsNotBooked()
    {
        var userId = Guid.NewGuid();
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.PickedUp, userId: userId);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var act = async () => await _bookingService.CancelBookingAsync(userId, 1);

        await act.Should().ThrowAsync<NotAllowedException>();
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelBooking_ShouldCancelBooking_WhenValid()
    {
        var userId = Guid.NewGuid();
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked, userId: userId);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.CancelBookingAsync(userId, 1);

        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Canceled);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
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
    public async Task SetBookingStatusAsync_ShouldSetBooked()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.PickedUp);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.Booked);

        result.Status.Should().Be(BookingStatus.Booked);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetCanceled()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.Canceled);

        result.Status.Should().Be(BookingStatus.Canceled);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetPickedUp()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.Booked);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.PickedUp);

        result.Status.Should().Be(BookingStatus.PickedUp);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetReturnLate()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.PickedUp);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.ReturnLate);

        result.Status.Should().Be(BookingStatus.ReturnLate);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetBookingStatusAsync_ShouldSetCompleted()
    {
        var booking = CreateBooking(id: 1, carId: 10, status: BookingStatus.ReturnLate);
        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(booking);

        var result = await _bookingService.SetBookingStatusAsync(1, BookingStatus.Completed);

        result.Status.Should().Be(BookingStatus.Completed);
        _mockBookingRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    #endregion

    private static Booking CreateBooking(int id, int carId, BookingStatus status, Guid userId = default)
    {
        var car = CreateCar(carId, CarStatus.Available);
        return new Booking
        {
            Id = id,
            CarId = carId,
            Car = car,
            UserId = userId,
            BookingDate = TestBaseDate.AddDays(id),
            PickupDate = TestBaseDate.AddDays(id + 1),
            DropoffDate = TestBaseDate.AddDays(id + 3),
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
