namespace Shared.Domain.Abstractions;

/// <summary>
/// Represents a domain event that occurs within the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Gets the identifier of the aggregate that raised this event.</summary>
    Guid AggregateId { get; }

    /// <summary>Gets the date and time when this event occurred.</summary>
    DateTime OccurredAt { get; }
}
