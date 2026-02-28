using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Shared.Infrastructure.Observability;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : (
                    System.Diagnostics.Activity.Current?.TraceId.ToString()
                    ?? context.TraceIdentifier
                );

        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        var scopeValues = new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.Value,
        };

        using var scope = _logger.BeginScope(scopeValues);
        using var correlationProperty = LogContext.PushProperty("CorrelationId", correlationId);
        using var pathProperty = LogContext.PushProperty("RequestPath", context.Request.Path.Value);

        await _next(context);
    }
}
