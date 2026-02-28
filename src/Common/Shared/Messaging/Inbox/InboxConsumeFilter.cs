namespace Shared.Infrastructure.Messaging.Inbox;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Observability;

/// <summary>
/// MassTransit consume filter that provides idempotent message processing.
/// Before a consumer runs, checks if the MessageId already exists in the InboxMessages table.
/// After successful processing, records the MessageId to prevent duplicate handling on redelivery.
/// </summary>
public class InboxConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IOutboxInboxDbContext _dbContext;
    private readonly ILogger<InboxConsumeFilter<T>> _logger;
    private readonly MessagingMetrics _messagingMetrics;

    public InboxConsumeFilter(
        IOutboxInboxDbContext dbContext,
        ILogger<InboxConsumeFilter<T>> logger,
        MessagingMetrics messagingMetrics
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _messagingMetrics = messagingMetrics;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageId = context.MessageId;
        if (messageId is null)
        {
            _logger.LogWarning(
                "Message of type {MessageType} has no MessageId, processing without inbox check",
                typeof(T).Name
            );
            await next.Send(context);
            return;
        }

        var alreadyProcessed = await _dbContext.InboxMessages.AnyAsync(
            x => x.MessageId == messageId.Value,
            context.CancellationToken
        );

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Duplicate message {MessageId} of type {MessageType} detected, skipping",
                messageId.Value,
                typeof(T).Name
            );

            _messagingMetrics.RecordInboxDuplicateSkipped(typeof(T).Name);
            return;
        }

        var startedAt = DateTime.UtcNow;
        await next.Send(context);

        _dbContext.InboxMessages.Add(
            new InboxMessage
            {
                Id = Guid.NewGuid(),
                MessageId = messageId.Value,
                ConsumerType = typeof(T).Name,
                ProcessedAtUtc = DateTime.UtcNow,
            }
        );

        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogDebug(
            "Recorded inbox entry for message {MessageId} of type {MessageType}",
            messageId.Value,
            typeof(T).Name
        );

        _messagingMetrics.RecordInboxConsumed(
            typeof(T).Name,
            (DateTime.UtcNow - startedAt).TotalMilliseconds
        );
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("inbox-dedup");
    }
}
