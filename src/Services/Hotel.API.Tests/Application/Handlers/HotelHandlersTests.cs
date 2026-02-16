namespace Hotel.API.Tests.Application.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hotel.API.Application.Commands;
using Hotel.API.Application.DTOs;
using Hotel.API.Application.Handlers;
using Hotel.API.Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.Domain.Abstractions;
using Xunit;

public class CreateHotelBookingCommandHandlerTests
{
    private readonly Mock<IRepository<HotelBooking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateHotelBookingCommandHandler _handler;

    public CreateHotelBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<HotelBooking>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreateHotelBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateHotelBooking_AndReturnMappedResponse()
    {
        // Arrange
        var command = new CreateHotelBookingCommand
        {
            UserId = Guid.NewGuid(),
            HotelName = "Grand Plaza Hotel",
            City = "San Francisco",
            CheckInDate = DateTime.UtcNow.Date.AddDays(7),
            CheckOutDate = DateTime.UtcNow.Date.AddDays(10),
            RoomCount = 2,
            PricePerNight = 150.00m
        };

        HotelBooking? capturedBooking = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<HotelBooking>(), It.IsAny<CancellationToken>()))
            .Callback<HotelBooking, CancellationToken>((b, _) => capturedBooking = b)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new HotelBookingResponseDto
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            HotelName = command.HotelName,
            City = command.City,
            Status = "Pending"
        };

        _mapperMock.Setup(m => m.Map<HotelBookingResponseDto>(It.IsAny<HotelBooking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        capturedBooking.Should().NotBeNull();
        capturedBooking!.UserId.Should().Be(command.UserId);
        capturedBooking.HotelName.Should().Be(command.HotelName);
        capturedBooking.City.Should().Be(command.City);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<HotelBooking>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(m => m.Map<HotelBookingResponseDto>(capturedBooking), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CreateHotelBookingWithCorrectProperties()
    {
        // Arrange
        var command = new CreateHotelBookingCommand
        {
            UserId = Guid.NewGuid(),
            HotelName = "Seaside Resort",
            City = "Miami",
            CheckInDate = DateTime.UtcNow.Date.AddDays(14),
            CheckOutDate = DateTime.UtcNow.Date.AddDays(21),
            RoomCount = 1,
            PricePerNight = 200.00m
        };

        HotelBooking? capturedBooking = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<HotelBooking>(), It.IsAny<CancellationToken>()))
            .Callback<HotelBooking, CancellationToken>((b, _) => capturedBooking = b)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<HotelBookingResponseDto>(It.IsAny<HotelBooking>()))
            .Returns(new HotelBookingResponseDto());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedBooking.Should().NotBeNull();
        capturedBooking!.Id.Should().NotBeEmpty();
        capturedBooking.RoomCount.Should().Be(command.RoomCount);
        capturedBooking.PricePerNight.Should().Be(command.PricePerNight);
    }
}

public class ConfirmHotelBookingCommandHandlerTests
{
    private readonly Mock<IRepository<HotelBooking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ConfirmHotelBookingCommandHandler _handler;

    public ConfirmHotelBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<HotelBooking>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new ConfirmHotelBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ConfirmHotelBooking_AndReturnMappedResponse()
    {
        // Arrange
        var hotelBookingId = Guid.NewGuid();
        var command = new ConfirmHotelBookingCommand { HotelBookingId = hotelBookingId };

        var hotelBooking = HotelBooking.Create(
            Guid.NewGuid(),
            "Downtown Inn",
            "Seattle",
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date.AddDays(7),
            1,
            175.00m);

        _repositoryMock.Setup(r => r.GetByIdAsync(hotelBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotelBooking);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HotelBooking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new HotelBookingResponseDto { Id = hotelBookingId, Status = "Confirmed" };
        _mapperMock.Setup(m => m.Map<HotelBookingResponseDto>(It.IsAny<HotelBooking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(hotelBookingId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(hotelBooking, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenHotelBookingNotFound()
    {
        // Arrange
        var command = new ConfirmHotelBookingCommand { HotelBookingId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdAsync(command.HotelBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HotelBooking?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}

public class CancelHotelBookingCommandHandlerTests
{
    private readonly Mock<IRepository<HotelBooking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CancelHotelBookingCommandHandler _handler;

    public CancelHotelBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<HotelBooking>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CancelHotelBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CancelHotelBooking_AndReturnMappedResponse()
    {
        // Arrange
        var hotelBookingId = Guid.NewGuid();
        var command = new CancelHotelBookingCommand { HotelBookingId = hotelBookingId };

        var hotelBooking = HotelBooking.Create(
            Guid.NewGuid(),
            "Mountain Lodge",
            "Denver",
            DateTime.UtcNow.Date.AddDays(3),
            DateTime.UtcNow.Date.AddDays(6),
            3,
            120.00m);

        _repositoryMock.Setup(r => r.GetByIdAsync(hotelBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotelBooking);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<HotelBooking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new HotelBookingResponseDto { Id = hotelBookingId, Status = "Cancelled" };
        _mapperMock.Setup(m => m.Map<HotelBookingResponseDto>(It.IsAny<HotelBooking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(hotelBookingId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(hotelBooking, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenHotelBookingNotFound()
    {
        // Arrange
        var command = new CancelHotelBookingCommand { HotelBookingId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdAsync(command.HotelBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HotelBooking?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}
