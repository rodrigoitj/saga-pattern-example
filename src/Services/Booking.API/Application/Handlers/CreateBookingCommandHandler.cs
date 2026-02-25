namespace Booking.API.Application.Handlers;

using AutoMapper;
using Booking.API.Application.Commands;
using Booking.API.Application.DTOs;
using Booking.API.Domain.Entities;
using MassTransit;
using MediatR;
using Shared.Domain.Abstractions;
using Shared.Domain.IntegrationEvents;

/// <summary>
/// Handler for CreateBookingCommand.
/// Implements CQRS - handles command execution.
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateBookingCommandHandler(
        IRepository<Booking> bookingRepository,
        IMapper mapper,
        IPublishEndpoint publishEndpoint
    )
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
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

        // Add to repository
        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

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

        await _publishEndpoint.Publish(integrationEvent, cancellationToken);

        return _mapper.Map<BookingResponseDto>(booking);
    }
}
