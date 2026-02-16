namespace Booking.API.Domain.Events;

using Shared.Domain.Abstractions;

public class BookingCreatedEvent : DomainEvent
{
    public Guid BookingId { get; init; }
    public Guid UserId { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
}

public class BookingConfirmedEvent : DomainEvent
{
    public Guid BookingId { get; init; }
}

public class BookingFailedEvent : DomainEvent
{
    public Guid BookingId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
