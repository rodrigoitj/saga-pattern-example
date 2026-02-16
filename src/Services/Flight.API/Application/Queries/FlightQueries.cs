namespace Flight.API.Application.Queries;

using MediatR;
using Flight.API.Application.DTOs;

public class GetFlightBookingByIdQuery : IRequest<FlightBookingResponseDto>
{
    public Guid FlightBookingId { get; init; }
}
