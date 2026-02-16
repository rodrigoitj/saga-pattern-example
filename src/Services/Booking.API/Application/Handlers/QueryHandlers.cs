namespace Booking.API.Application.Handlers;

using AutoMapper;
using MediatR;
using Booking.API.Application.DTOs;
using Booking.API.Application.Queries;
using Booking.API.Domain.Entities;
using Shared.Domain.Abstractions;

/// <summary>
/// Handler for GetBookingByIdQuery.
/// Implements CQRS - handles query execution.
/// </summary>
public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingResponseDto>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMapper _mapper;

    public GetBookingByIdQueryHandler(
        IRepository<Booking> bookingRepository,
        IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
    }

    public async Task<BookingResponseDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        
        if (booking is null)
        {
            throw new InvalidOperationException($"Booking with ID {request.BookingId} not found");
        }

        return _mapper.Map<BookingResponseDto>(booking);
    }
}

/// <summary>
/// Handler for GetUserBookingsQuery.
/// </summary>
public class GetUserBookingsQueryHandler : IRequestHandler<GetUserBookingsQuery, List<BookingResponseDto>>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IMapper _mapper;

    public GetUserBookingsQueryHandler(
        IRepository<Booking> bookingRepository,
        IMapper mapper)
    {
        _bookingRepository = bookingRepository;
        _mapper = mapper;
    }

    public async Task<List<BookingResponseDto>> Handle(GetUserBookingsQuery request, CancellationToken cancellationToken)
    {
        var bookings = await _bookingRepository.GetAllAsync(cancellationToken);
        var userBookings = bookings.Where(b => b.UserId == request.UserId).ToList();

        return _mapper.Map<List<BookingResponseDto>>(userBookings);
    }
}
