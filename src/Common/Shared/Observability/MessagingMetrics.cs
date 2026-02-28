using System.Diagnostics.Metrics;

namespace Shared.Infrastructure.Observability;

public sealed class MessagingMetrics
{
    private readonly Counter<long> _outboxEnqueuedTotal;
    private readonly Counter<long> _outboxPublishedTotal;
    private readonly Counter<long> _outboxPublishFailedTotal;
    private readonly Histogram<double> _outboxPublishDurationMs;
    private readonly Counter<long> _inboxConsumedTotal;
    private readonly Counter<long> _inboxDuplicateSkippedTotal;
    private readonly Histogram<double> _inboxConsumeDurationMs;

    public MessagingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(TelemetryConstants.MessagingMeterName);

        _outboxEnqueuedTotal = meter.CreateCounter<long>("messaging.outbox.enqueued.total");
        _outboxPublishedTotal = meter.CreateCounter<long>("messaging.outbox.published.total");
        _outboxPublishFailedTotal = meter.CreateCounter<long>(
            "messaging.outbox.publish.failed.total"
        );
        _outboxPublishDurationMs = meter.CreateHistogram<double>(
            "messaging.outbox.publish.duration.ms",
            unit: "ms"
        );
        _inboxConsumedTotal = meter.CreateCounter<long>("messaging.inbox.consumed.total");
        _inboxDuplicateSkippedTotal = meter.CreateCounter<long>(
            "messaging.inbox.duplicate_skipped.total"
        );
        _inboxConsumeDurationMs = meter.CreateHistogram<double>(
            "messaging.inbox.consume.duration.ms",
            unit: "ms"
        );
    }

    public void RecordOutboxEnqueued(string messageType) =>
        _outboxEnqueuedTotal.Add(1, new KeyValuePair<string, object?>("message.type", messageType));

    public void RecordOutboxPublished(string messageType, double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[] { new("message.type", messageType) };

        _outboxPublishedTotal.Add(1, tags);
        _outboxPublishDurationMs.Record(durationMs, tags);
    }

    public void RecordOutboxPublishFailed(string messageType)
    {
        _outboxPublishFailedTotal.Add(
            1,
            new KeyValuePair<string, object?>("message.type", messageType)
        );
    }

    public void RecordInboxConsumed(string messageType, double durationMs)
    {
        var tags = new KeyValuePair<string, object?>[] { new("message.type", messageType) };

        _inboxConsumedTotal.Add(1, tags);
        _inboxConsumeDurationMs.Record(durationMs, tags);
    }

    public void RecordInboxDuplicateSkipped(string messageType)
    {
        _inboxDuplicateSkippedTotal.Add(
            1,
            new KeyValuePair<string, object?>("message.type", messageType)
        );
    }
}
