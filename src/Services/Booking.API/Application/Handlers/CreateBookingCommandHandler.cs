namespace Booking.API.Application.Handlers;

using AutoMapper;
using Booking.API.Application.Commands;
using Booking.API.Application.DTOs;
using Booking.API.Application.Observability;
using Booking.API.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly BookingMetrics? _bookingMetrics;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IRepository<Booking> bookingRepository,
        IMapper mapper,
        IOutboxPublisher outboxPublisher,
        BookingMetrics? bookingMetrics = null,
        ILogger<CreateBookingCommandHandler>? logger = null
    )
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
        _outboxPublisher = outboxPublisher;
        _bookingMetrics = bookingMetrics;
        _logger = logger ?? NullLogger<CreateBookingCommandHandler>.Instance;
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

        _bookingMetrics?.RecordBookingCreated(
            request.IncludeFlights,
            request.IncludeHotel,
            request.IncludeCar
        );

        _logger.LogInformation(
            "Booking created and event enqueued for processing. BookingId: {BookingId}, ReferenceNumber: {ReferenceNumber}",
            booking.Id,
            booking.ReferenceNumber
        );

        return _mapper.Map<BookingResponseDto>(booking);
    }
}
