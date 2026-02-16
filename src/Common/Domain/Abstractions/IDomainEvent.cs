namespace Shared.Domain.Abstractions;

/// <summary>
/// Represents a domain event that occurs within the domain.
/// </summary>
public interface IDomainEvent
{
    Guid AggregateId { get; }
    DateTime OccurredAt { get; }
}
