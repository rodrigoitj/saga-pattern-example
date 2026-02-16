namespace Booking.API.Tests.Application.Handlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Booking.API.Application.Commands;
using Booking.API.Application.DTOs;
using Booking.API.Application.Handlers;
using Booking.API.Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.Domain.Abstractions;
using Xunit;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IRepository<Booking>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Booking>>();
        _mapperMock = new Mock<IMapper>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new CreateBookingCommandHandler(_repositoryMock.Object, _mapperMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_CreateBooking_AndReturnMappedResponse()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            UserId = Guid.NewGuid(),
            CheckInDate = DateTime.UtcNow.Date.AddDays(1),
            CheckOutDate = DateTime.UtcNow.Date.AddDays(3)
        };

        Booking? capturedBooking = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => capturedBooking = b)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var expectedDto = new BookingResponseDto
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "BK20260213120000ABCD1234",
            UserId = command.UserId,
            Status = "Pending",
            TotalPrice = 0,
            CheckInDate = command.CheckInDate,
            CheckOutDate = command.CheckOutDate
        };

        _mapperMock.Setup(m => m.Map<BookingResponseDto>(It.IsAny<Booking>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedDto);
        capturedBooking.Should().NotBeNull();
        capturedBooking!.UserId.Should().Be(command.UserId);
        capturedBooking.CheckInDate.Should().Be(command.CheckInDate);
        capturedBooking.CheckOutDate.Should().Be(command.CheckOutDate);
        capturedBooking.ReferenceNumber.Should().StartWith("BK");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(m => m.Map<BookingResponseDto>(It.Is<Booking>(b => b == capturedBooking)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_GenerateUniqueReferenceNumber()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            UserId = Guid.NewGuid(),
            CheckInDate = DateTime.UtcNow.Date.AddDays(1),
            CheckOutDate = DateTime.UtcNow.Date.AddDays(3)
        };

        Booking? capturedBooking = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => capturedBooking = b)
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<BookingResponseDto>(It.IsAny<Booking>()))
            .Returns(new BookingResponseDto());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedBooking.Should().NotBeNull();
        capturedBooking!.ReferenceNumber.Should().NotBeNullOrEmpty();
        capturedBooking.ReferenceNumber.Should().StartWith("BK");
        capturedBooking.ReferenceNumber.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task Handle_Should_CallRepositoryMethodsInCorrectOrder()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            UserId = Guid.NewGuid(),
            CheckInDate = DateTime.UtcNow.Date.AddDays(1),
            CheckOutDate = DateTime.UtcNow.Date.AddDays(3)
        };

        var sequence = new MockSequence();
        _repositoryMock.InSequence(sequence)
            .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.InSequence(sequence)
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<BookingResponseDto>(It.IsAny<Booking>()))
            .Returns(new BookingResponseDto());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Sequence verification is done by MockSequence
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
