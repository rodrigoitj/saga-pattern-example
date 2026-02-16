namespace Flight.API.Tests.Application.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Flight.API.Application.Commands;
using Flight.API.Application.DTOs;
using Flight.API.Application.Handlers;
using Flight.API.Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.Domain.Abstractions;
using Xunit;

public class CreateFlightBookingCommandHandlerTests
{
    private readonly Mock<IRepository<FlightBooking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateFlightBookingCommandHandler _handler;

    public CreateFlightBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<FlightBooking>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreateFlightBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateFlightBooking_AndReturnMappedResponse()
    {
        // Arrange
        var command = new CreateFlightBookingCommand
        {
            UserId = Guid.NewGuid(),
            DepartureCity = "New York",
            ArrivalCity = "Los Angeles",
            DepartureDateUtc = DateTime.UtcNow.AddDays(7),
            ArrivalDateUtc = DateTime.UtcNow.AddDays(7).AddHours(5),
            Price = 350.00m,
            PassengerCount = 2
        };

        FlightBooking? capturedBooking = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<FlightBooking>(), It.IsAny<CancellationToken>()))
            .Callback<FlightBooking, CancellationToken>((b, _) => capturedBooking = b)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new FlightBookingResponseDto
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            DepartureCity = command.DepartureCity,
            ArrivalCity = command.ArrivalCity,
            Price = command.Price,
            Status = "Pending"
        };

        _mapperMock.Setup(m => m.Map<FlightBookingResponseDto>(It.IsAny<FlightBooking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        capturedBooking.Should().NotBeNull();
        capturedBooking!.UserId.Should().Be(command.UserId);
        capturedBooking.DepartureCity.Should().Be(command.DepartureCity);
        capturedBooking.ArrivalCity.Should().Be(command.ArrivalCity);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<FlightBooking>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(m => m.Map<FlightBookingResponseDto>(capturedBooking), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CreateFlightBookingWithCorrectProperties()
    {
        // Arrange
        var command = new CreateFlightBookingCommand
        {
            UserId = Guid.NewGuid(),
            DepartureCity = "Chicago",
            ArrivalCity = "Miami",
            DepartureDateUtc = DateTime.UtcNow.AddDays(10),
            ArrivalDateUtc = DateTime.UtcNow.AddDays(10).AddHours(3),
            Price = 280.00m,
            PassengerCount = 1
        };

        FlightBooking? capturedBooking = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<FlightBooking>(), It.IsAny<CancellationToken>()))
            .Callback<FlightBooking, CancellationToken>((b, _) => capturedBooking = b)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<FlightBookingResponseDto>(It.IsAny<FlightBooking>()))
            .Returns(new FlightBookingResponseDto());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedBooking.Should().NotBeNull();
        capturedBooking!.Id.Should().NotBeEmpty();
        capturedBooking.PassengerCount.Should().Be(command.PassengerCount);
        capturedBooking.Price.Should().Be(command.Price);
    }
}

public class ConfirmFlightBookingCommandHandlerTests
{
    private readonly Mock<IRepository<FlightBooking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ConfirmFlightBookingCommandHandler _handler;

    public ConfirmFlightBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<FlightBooking>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new ConfirmFlightBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ConfirmFlightBooking_AndReturnMappedResponse()
    {
        // Arrange
        var flightBookingId = Guid.NewGuid();
        var command = new ConfirmFlightBookingCommand { FlightBookingId = flightBookingId };

        var flightBooking = FlightBooking.Create(
            Guid.NewGuid(),
            "Boston",
            "Seattle",
            DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(5).AddHours(6),
            420.00m,
            1);

        _repositoryMock.Setup(r => r.GetByIdAsync(flightBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flightBooking);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<FlightBooking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new FlightBookingResponseDto { Id = flightBookingId, Status = "Confirmed" };
        _mapperMock.Setup(m => m.Map<FlightBookingResponseDto>(It.IsAny<FlightBooking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(flightBookingId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(flightBooking, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenFlightBookingNotFound()
    {
        // Arrange
        var command = new ConfirmFlightBookingCommand { FlightBookingId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdAsync(command.FlightBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightBooking?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}

public class CancelFlightBookingCommandHandlerTests
{
    private readonly Mock<IRepository<FlightBooking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CancelFlightBookingCommandHandler _handler;

    public CancelFlightBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<FlightBooking>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CancelFlightBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CancelFlightBooking_AndReturnMappedResponse()
    {
        // Arrange
        var flightBookingId = Guid.NewGuid();
        var command = new CancelFlightBookingCommand { FlightBookingId = flightBookingId };

        var flightBooking = FlightBooking.Create(
            Guid.NewGuid(),
            "Dallas",
            "Denver",
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(3).AddHours(2),
            250.00m,
            2);

        _repositoryMock.Setup(r => r.GetByIdAsync(flightBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flightBooking);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<FlightBooking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new FlightBookingResponseDto { Id = flightBookingId, Status = "Cancelled" };
        _mapperMock.Setup(m => m.Map<FlightBookingResponseDto>(It.IsAny<FlightBooking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(flightBookingId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(flightBooking, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenFlightBookingNotFound()
    {
        // Arrange
        var command = new CancelFlightBookingCommand { FlightBookingId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdAsync(command.FlightBookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlightBooking?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}
