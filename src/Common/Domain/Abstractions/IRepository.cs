namespace Shared.Domain.Abstractions;

/// <summary>
/// Repository interface for generic data access operations.
/// Follows the Repository Pattern for data access abstraction.
/// </summary>
public interface IRepository<T>
    where T : AggregateRoot
{
    /// <summary>Gets an entity by its unique identifier.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all entities.</summary>
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new entity to the repository.</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing entity in the repository.</summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Deletes an entity from the repository.</summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Persists all pending changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
