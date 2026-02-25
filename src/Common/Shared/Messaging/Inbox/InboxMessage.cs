namespace Shared.Infrastructure.Messaging.Inbox;

public class InboxMessage
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string ConsumerType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
