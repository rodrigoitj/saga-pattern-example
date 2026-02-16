namespace Hotel.API.Domain.Events;

using Shared.Domain.Abstractions;

public class HotelBookedEvent : DomainEvent
{
    public Guid HotelBookingId { get; init; }
    public Guid UserId { get; init; }
    public string ConfirmationCode { get; init; } = string.Empty;
}
