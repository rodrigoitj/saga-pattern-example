namespace Shared.Domain.Abstractions;

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <inheritdoc/>
    public Guid AggregateId { get; init; }
    /// <inheritdoc/>
    public DateTime OccurredAt { get; protected init; } = DateTime.UtcNow;
}
