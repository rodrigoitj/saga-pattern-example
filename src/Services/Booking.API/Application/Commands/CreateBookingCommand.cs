namespace Booking.API.Application.Commands;

using MediatR;
using Booking.API.Application.DTOs;

/// <summary>
/// Command to initiate a booking with flights, hotels, and cars.
/// Follows CQRS pattern - command for write operations.
/// </summary>
public class CreateBookingCommand : IRequest<BookingResponseDto>
{
    public Guid UserId { get; init; }
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public bool IncludeFlights { get; init; }
    public bool IncludeHotel { get; init; }
    public bool IncludeCar { get; init; }
}
