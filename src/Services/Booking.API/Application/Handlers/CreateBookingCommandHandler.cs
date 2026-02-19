namespace Booking.API.Application.Handlers;

using System.Linq;
using AutoMapper;
using MediatR;
using Booking.API.Application.Commands;
using Booking.API.Application.DTOs;
using Booking.API.Domain.Entities;
using Shared.Domain.Abstractions;

/// <summary>
/// Handler for CreateBookingCommand.
/// Implements CQRS - handles command execution.
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingResponseDto>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;

    public CreateBookingCommandHandler(
        IRepository<Booking> bookingRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IPublisher publisher)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<BookingResponseDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // Generate reference number
        var referenceNumber = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // Create booking aggregate
        var booking = Booking.Create(
            request.UserId,
            request.CheckInDate,
            request.CheckOutDate,
            referenceNumber);

        // Add to repository
        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);

        // Publish domain events to trigger saga orchestration
        foreach (var domainEvent in booking.DomainEvents.ToList())
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        // Clear domain events after publishing
        booking.ClearDomainEvents();

        return _mapper.Map<BookingResponseDto>(booking);
    }
}
