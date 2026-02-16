namespace Car.API.Tests.Application.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Car.API.Application.Commands;
using Car.API.Application.DTOs;
using Car.API.Application.Handlers;
using Car.API.Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.Domain.Abstractions;
using Xunit;

public class CreateCarRentalCommandHandlerTests
{
    private readonly Mock<IRepository<CarRental>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateCarRentalCommandHandler _handler;

    public CreateCarRentalCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CarRental>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreateCarRentalCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateCarRental_AndReturnMappedResponse()
    {
        // Arrange
        var command = new CreateCarRentalCommand
        {
            UserId = Guid.NewGuid(),
            CarModel = "Toyota Camry",
            Company = "Enterprise",
            PickUpDate = DateTime.UtcNow.Date.AddDays(7),
            ReturnDate = DateTime.UtcNow.Date.AddDays(10),
            PickUpLocation = "Los Angeles Airport",
            PricePerDay = 45.00m
        };

        CarRental? capturedRental = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<CarRental>(), It.IsAny<CancellationToken>()))
            .Callback<CarRental, CancellationToken>((c, _) => capturedRental = c)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new CarRentalResponseDto
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            CarModel = command.CarModel,
            Company = command.Company,
            Status = "Pending"
        };

        _mapperMock.Setup(m => m.Map<CarRentalResponseDto>(It.IsAny<CarRental>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        capturedRental.Should().NotBeNull();
        capturedRental!.UserId.Should().Be(command.UserId);
        capturedRental.CarModel.Should().Be(command.CarModel);
        capturedRental.Company.Should().Be(command.Company);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<CarRental>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(m => m.Map<CarRentalResponseDto>(capturedRental), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_CreateCarRentalWithCorrectProperties()
    {
        // Arrange
        var command = new CreateCarRentalCommand
        {
            UserId = Guid.NewGuid(),
            CarModel = "Honda Accord",
            Company = "Hertz",
            PickUpDate = DateTime.UtcNow.Date.AddDays(14),
            ReturnDate = DateTime.UtcNow.Date.AddDays(21),
            PickUpLocation = "Miami Downtown",
            PricePerDay = 50.00m
        };

        CarRental? capturedRental = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<CarRental>(), It.IsAny<CancellationToken>()))
            .Callback<CarRental, CancellationToken>((c, _) => capturedRental = c)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<CarRentalResponseDto>(It.IsAny<CarRental>()))
            .Returns(new CarRentalResponseDto());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRental.Should().NotBeNull();
        capturedRental!.Id.Should().NotBeEmpty();
        capturedRental.PickUpLocation.Should().Be(command.PickUpLocation);
        capturedRental.PricePerDay.Should().Be(command.PricePerDay);
    }
}

public class ConfirmCarRentalCommandHandlerTests
{
    private readonly Mock<IRepository<CarRental>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ConfirmCarRentalCommandHandler _handler;

    public ConfirmCarRentalCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CarRental>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new ConfirmCarRentalCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ConfirmCarRental_AndReturnMappedResponse()
    {
        // Arrange
        var carRentalId = Guid.NewGuid();
        var command = new ConfirmCarRentalCommand { CarRentalId = carRentalId };

        var carRental = CarRental.Create(
            Guid.NewGuid(),
            "Ford Mustang",
            "Budget",
            DateTime.UtcNow.Date.AddDays(5),
            DateTime.UtcNow.Date.AddDays(8),
            "Chicago O'Hare",
            60.00m);

        _repositoryMock.Setup(r => r.GetByIdAsync(carRentalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(carRental);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CarRental>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new CarRentalResponseDto { Id = carRentalId, Status = "Confirmed" };
        _mapperMock.Setup(m => m.Map<CarRentalResponseDto>(It.IsAny<CarRental>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(carRentalId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(carRental, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenCarRentalNotFound()
    {
        // Arrange
        var command = new ConfirmCarRentalCommand { CarRentalId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdAsync(command.CarRentalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CarRental?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}

public class CancelCarRentalCommandHandlerTests
{
    private readonly Mock<IRepository<CarRental>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CancelCarRentalCommandHandler _handler;

    public CancelCarRentalCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CarRental>>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CancelCarRentalCommandHandler(_repositoryMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CancelCarRental_AndReturnMappedResponse()
    {
        // Arrange
        var carRentalId = Guid.NewGuid();
        var command = new CancelCarRentalCommand { CarRentalId = carRentalId };

        var carRental = CarRental.Create(
            Guid.NewGuid(),
            "Chevrolet Malibu",
            "Avis",
            DateTime.UtcNow.Date.AddDays(3),
            DateTime.UtcNow.Date.AddDays(6),
            "Denver Downtown",
            55.00m);

        _repositoryMock.Setup(r => r.GetByIdAsync(carRentalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(carRental);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<CarRental>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new CarRentalResponseDto { Id = carRentalId, Status = "Cancelled" };
        _mapperMock.Setup(m => m.Map<CarRentalResponseDto>(It.IsAny<CarRental>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(carRentalId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(carRental, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenCarRentalNotFound()
    {
        // Arrange
        var command = new CancelCarRentalCommand { CarRentalId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdAsync(command.CarRentalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CarRental?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}
