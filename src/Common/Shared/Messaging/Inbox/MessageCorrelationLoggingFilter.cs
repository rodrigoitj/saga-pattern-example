using MassTransit;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Messaging.Inbox;

public sealed class MessageCorrelationLoggingFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly ILogger<MessageCorrelationLoggingFilter<T>> _logger;

    public MessageCorrelationLoggingFilter(ILogger<MessageCorrelationLoggingFilter<T>> logger)
    {
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationId =
            context.CorrelationId?.ToString()
            ?? context.ConversationId?.ToString()
            ?? context.MessageId?.ToString()
            ?? System.Diagnostics.Activity.Current?.TraceId.ToString();

        var scopeValues = new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["MessageId"] = context.MessageId,
            ["ConversationId"] = context.ConversationId,
            ["MessageType"] = typeof(T).Name,
        };

        using var scope = _logger.BeginScope(scopeValues);
        using var correlationProperty = Serilog.Context.LogContext.PushProperty(
            "CorrelationId",
            correlationId
        );
        using var messageIdProperty = Serilog.Context.LogContext.PushProperty(
            "MessageId",
            context.MessageId?.ToString()
        );
        using var conversationProperty = Serilog.Context.LogContext.PushProperty(
            "ConversationId",
            context.ConversationId?.ToString()
        );

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("message-correlation");
    }
}
