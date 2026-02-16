namespace Shared.Domain.Abstractions;

/// <summary>
/// Unit of Work pattern interface for managing transactions.
/// </summary>
public interface IUnitOfWork
{
    IRepository<T> GetRepository<T>() where T : AggregateRoot;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
