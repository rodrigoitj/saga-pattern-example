namespace Shared.Infrastructure.Messaging.Outbox;

using System.Text.Json;
using Shared.Infrastructure.Messaging.Configuration;

/// <summary>
/// Saves messages to the OutboxMessages table instead of publishing directly to RabbitMQ.
/// The message is added to the DbContext change tracker — the caller must call SaveChangesAsync
/// to persist both business data and the outbox message atomically in a single transaction.
/// </summary>
public class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxInboxDbContext _dbContext;

    public OutboxPublisher(IOutboxInboxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}",
            Content = JsonSerializer.Serialize(message),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
