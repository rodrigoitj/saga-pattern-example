namespace Car.API.Domain.Events;

using Shared.Domain.Abstractions;

public class CarRentalBookedEvent : DomainEvent
{
    public Guid CarRentalId { get; init; }
    public Guid UserId { get; init; }
    public string ReservationCode { get; init; } = string.Empty;
}
