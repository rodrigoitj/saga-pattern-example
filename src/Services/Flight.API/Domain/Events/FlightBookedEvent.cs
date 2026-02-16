namespace Flight.API.Domain.Events;

using Shared.Domain.Abstractions;

public class FlightBookedEvent : DomainEvent
{
    public Guid FlightBookingId { get; init; }
    public Guid UserId { get; init; }
    public string ConfirmationCode { get; init; } = string.Empty;
}
