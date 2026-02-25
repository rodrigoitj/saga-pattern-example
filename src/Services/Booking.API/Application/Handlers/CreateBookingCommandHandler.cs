namespace Booking.API.Application.Handlers;

using AutoMapper;
using Booking.API.Application.Commands;
using Booking.API.Application.DTOs;
using Booking.API.Domain.Entities;
using MediatR;
using Shared.Domain.Abstractions;
using Shared.Domain.IntegrationEvents;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Handler for CreateBookingCommand.
/// Implements CQRS - handles command execution.
/// Uses the outbox pattern: the integration event is saved to the OutboxMessages table
/// in the same transaction as the booking, ensuring reliable message delivery.
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMapper _mapper;
    private readonly IOutboxPublisher _outboxPublisher;

    public CreateBookingCommandHandler(
        IRepository<Booking> bookingRepository,
        IMapper mapper,
        IOutboxPublisher outboxPublisher
    )
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
        _outboxPublisher = outboxPublisher;
    }

    public async Task<BookingResponseDto> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken
    )
    {
        // Generate reference number
        var referenceNumber =
            $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Create booking aggregate
        var booking = Booking.Create(
            request.UserId,
            request.CheckInDate,
            request.CheckOutDate,
            referenceNumber,
            request.IncludeFlights,
            request.IncludeHotel,
            request.IncludeCar
        );
        booking.MarkAsProcessing();

        // Add booking and outbox message to the same DbContext change tracker,
        // then SaveChanges persists both atomically in a single transaction.
        await _bookingRepository.AddAsync(booking, cancellationToken);

        var integrationEvent = new BookingCreatedIntegrationEvent
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            IncludeFlights = booking.IncludeFlights,
            IncludeHotel = booking.IncludeHotel,
            IncludeCar = booking.IncludeCar,
            ReferenceNumber = booking.ReferenceNumber,
        };

        await _outboxPublisher.PublishAsync(integrationEvent, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BookingResponseDto>(booking);
    }
}
