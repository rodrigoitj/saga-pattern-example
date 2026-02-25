namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Adds an outbox message to the DbContext change tracker.
/// The caller must call SaveChangesAsync to persist both business data and the outbox message atomically.
/// </summary>
public interface IOutboxPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}
