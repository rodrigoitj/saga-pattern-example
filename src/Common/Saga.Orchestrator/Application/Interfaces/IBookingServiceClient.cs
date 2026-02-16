namespace Saga.Orchestrator.Application.Interfaces;

using Saga.Orchestrator.Domain.Models;

/// <summary>
/// Interface for booking request to external services.
/// </summary>
public interface IBookingServiceClient
{
    Task<FlightBookingResult> BookFlightAsync(FlightBookingRequest request, CancellationToken cancellationToken);
    Task<HotelBookingResult> BookHotelAsync(HotelBookingRequest request, CancellationToken cancellationToken);
    Task<CarBookingResult> BookCarAsync(CarBookingRequest request, CancellationToken cancellationToken);

    Task<bool> CancelFlightAsync(Guid flightBookingId, CancellationToken cancellationToken);
    Task<bool> CancelHotelAsync(Guid hotelBookingId, CancellationToken cancellationToken);
    Task<bool> CancelCarAsync(Guid carBookingId, CancellationToken cancellationToken);
}
