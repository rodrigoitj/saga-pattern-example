namespace Flight.API.Application.Commands;

using MediatR;
using Flight.API.Application.DTOs;

public class CreateFlightBookingCommand : IRequest<FlightBookingResponseDto>
{
    public Guid UserId { get; init; }
    public string DepartureCity { get; init; } = string.Empty;
    public string ArrivalCity { get; init; } = string.Empty;
    public DateTime DepartureDateUtc { get; init; }
    public DateTime ArrivalDateUtc { get; init; }
    public decimal Price { get; init; }
    public int PassengerCount { get; init; }
}

public class ConfirmFlightBookingCommand : IRequest<FlightBookingResponseDto>
{
    public Guid FlightBookingId { get; init; }
}

public class CancelFlightBookingCommand : IRequest<FlightBookingResponseDto>
{
    public Guid FlightBookingId { get; init; }
}
