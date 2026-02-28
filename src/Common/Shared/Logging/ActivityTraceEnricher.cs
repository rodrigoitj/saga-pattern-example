using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Shared.Infrastructure.Logging;

public sealed class ActivityTraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        var traceId = activity.TraceId.ToString();
        var spanId = activity.SpanId.ToString();

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", spanId));
    }
}
