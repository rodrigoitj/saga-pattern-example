namespace Shared.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots.
/// Aggregates are clusters of domain objects that can be treated as a unit.
/// </summary>
public abstract class AggregateRoot : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class.
    /// </summary>
    protected AggregateRoot() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRoot"/> class with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the aggregate root.</param>
    protected AggregateRoot(Guid id)
        : base(id) { }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets a read-only collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents
    {
        get { return _domainEvents.AsReadOnly(); }
    }

    /// <summary>
    /// Raises a domain event by adding it to the collection of domain events.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events that have been raised by this aggregate.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
