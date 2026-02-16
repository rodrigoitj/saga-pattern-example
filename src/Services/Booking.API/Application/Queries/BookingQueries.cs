namespace Booking.API.Application.Queries;

using MediatR;
using Booking.API.Application.DTOs;

/// <summary>
/// Query to retrieve a booking by ID.
/// Follows CQRS pattern - query for read operations.
/// </summary>
public class GetBookingByIdQuery : IRequest<BookingResponseDto>
{
    public Guid BookingId { get; init; }
}

/// <summary>
/// Query to retrieve all bookings for a user.
/// </summary>
public class GetUserBookingsQuery : IRequest<List<BookingResponseDto>>
{
    public Guid UserId { get; init; }
}
