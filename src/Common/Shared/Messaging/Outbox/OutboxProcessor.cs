namespace Shared.Infrastructure.Messaging.Outbox;

using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Observability;

/// <summary>
/// Background service that periodically polls the OutboxMessages table for unprocessed messages
/// and publishes them to RabbitMQ via MassTransit. After successful publishing, the message is
/// marked as processed. Failed messages have their error recorded and retry count incremented.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly MessagingMetrics _messagingMetrics;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private const int MaxRetryCount = 5;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger,
        MessagingMetrics messagingMetrics
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _messagingMetrics = messagingMetrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor stopped");
    }

    private async Task ProcessPendingMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOutboxInboxDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await dbContext
            .OutboxMessages.Where(m => m.ProcessedAtUtc == null && m.RetryCount < MaxRetryCount)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var startedAt = DateTime.UtcNow;
                var messageType = Type.GetType(message.Type);
                if (messageType is null)
                {
                    _logger.LogWarning(
                        "Could not resolve type {Type} for outbox message {Id}",
                        message.Type,
                        message.Id
                    );
                    message.Error = $"Could not resolve type: {message.Type}";
                    message.RetryCount = MaxRetryCount; // permanent failure
                    _messagingMetrics.RecordOutboxPublishFailed(message.Type);
                    continue;
                }

                var payload = JsonSerializer.Deserialize(message.Content, messageType);
                if (payload is null)
                {
                    _logger.LogWarning(
                        "Deserialization returned null for outbox message {Id}",
                        message.Id
                    );
                    message.Error = "Deserialization returned null";
                    message.RetryCount = MaxRetryCount; // permanent failure
                    _messagingMetrics.RecordOutboxPublishFailed(message.Type);
                    continue;
                }

                await publishEndpoint.Publish(payload, messageType, cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.Error = null;

                _logger.LogDebug(
                    "Published outbox message {Id} of type {Type}",
                    message.Id,
                    message.Type
                );

                _messagingMetrics.RecordOutboxPublished(
                    messageType.Name,
                    (DateTime.UtcNow - startedAt).TotalMilliseconds
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
                message.Error = ex.Message;
                message.RetryCount++;
                _messagingMetrics.RecordOutboxPublishFailed(message.Type);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
