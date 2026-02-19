namespace Shared.Domain.Abstractions;

using MediatR;

/// <summary>
/// Base class for domain events.
/// Implements INotification to enable MediatR event publishing.
/// </summary>
public abstract class DomainEvent : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public Guid AggregateId { get; init; }
    /// <inheritdoc/>
    public DateTime OccurredAt { get; protected init; } = DateTime.UtcNow;
}
