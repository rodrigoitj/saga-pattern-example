namespace Shared.Infrastructure.Messaging.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
