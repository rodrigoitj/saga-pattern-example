# Observability Guide (OpenTelemetry + Serilog)

This project now includes a shared observability foundation for all services.

## Recommended NuGet Packages

Added in shared infrastructure project:

- `OpenTelemetry.Extensions.Hosting`
- `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- `OpenTelemetry.Exporter.Console`
- `OpenTelemetry.Exporter.Prometheus.AspNetCore`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.Http`
- `OpenTelemetry.Instrumentation.EntityFrameworkCore`
- `OpenTelemetry.Instrumentation.Runtime`
- `OpenTelemetry.Instrumentation.Process`
- `Serilog.AspNetCore`
- `Serilog.Settings.Configuration`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.OpenTelemetry`
- `Serilog.Enrichers.Environment`
- `Serilog.Enrichers.Thread`

## Dependency Injection / Program Setup

Each service now registers shared observability in `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("booking-api");

var app = builder.Build();
app.UseObservability();
```

This configures:

- Traces: ASP.NET Core, outgoing `HttpClient`, EF Core, MassTransit source.
- Metrics: HTTP server/client, runtime/process metrics, custom domain/message meters.
- Logs: Serilog structured logging enriched with trace/span and correlation fields.
- Exporters: environment-driven Console/Prometheus/OTLP.

## Custom Business Metrics Example

`Booking.API` now emits custom counters in `Application/Observability/BookingMetrics.cs`:

- `booking.created.total`
- `booking.step.completed.total`
- `booking.confirmed.total`
- `booking.failed.total`

Recorded from:

- `CreateBookingCommandHandler`
- `BookingStepCompletedConsumer`
- `BookingFailedConsumer`

## Correlated Structured Logging Example

Correlation is propagated via:

- HTTP middleware: `X-Correlation-Id` + log scope + Serilog log context
- Message consume filter: correlation/message identifiers pushed into log context
- Custom activity enricher: adds `TraceId` and `SpanId` from current activity

This enables joining logs with traces and metric spikes using shared identifiers (`TraceId`, `SpanId`, `CorrelationId`, `BookingId`).

## Exporters by Environment

### Local Development

`appsettings.Development.json` uses:

- Console exporter: enabled
- Prometheus exporter: enabled (`/metrics`)
- OTLP exporter: disabled

Local stack includes:

- Prometheus (`http://localhost:9090`)
- Grafana (`http://localhost:3000`)
- Tempo (`http://localhost:3200`)
- OTel Collector (`4317/4318`) for optional OTLP testing

### Production

`appsettings.json` uses:

- Console exporter: disabled
- Prometheus exporter: disabled
- OTLP exporter: enabled (`http://otel-collector:4317`)

Collector config can fan out telemetry to Tempo/Jaeger/Grafana backends.

## Production Best Practices

1. **Sampling**
   - Use parent-based ratio sampling (already configured).
   - Start around 10-20% for high-throughput traffic; adjust by SLO/error budget.

2. **Cardinality control**
   - Keep metric tags low-cardinality (`step`, boolean flags, service names).
   - Avoid user IDs, booking references, and GUIDs as metric labels.

3. **Naming conventions**
   - Use stable, dot-separated names (e.g., `booking.created.total`).
   - Prefer semantic convention names for platform telemetry; domain prefix for business metrics.

4. **PII safety**
   - Do not enable raw SQL statement capture in production unless audited.
   - Keep logs structured and avoid sensitive payload dumps.

5. **Collector-first architecture**
   - Apps export OTLP to collector only.
   - Collector handles routing, retries, transforms, and vendor-specific exporters.

6. **Kubernetes readiness**
   - Keep endpoint/protocol/headers configurable via env vars.
   - Expose `/metrics` only when needed for scrape-based deployments.
   - Use sidecar/daemonset collector patterns for cluster rollout.

## Suggested Folder Structure

```text
observability/
  otel-collector-config.yaml
  prometheus.yml
  tempo.yaml
  grafana-datasources.yaml
  grafana-dashboards.yaml
  dashboards/

src/Common/Shared/
  Observability/
    ObservabilityOptions.cs
    TelemetryConstants.cs
    MessagingMetrics.cs
    CorrelationIdMiddleware.cs
  Extensions/
    ObservabilityExtensions.cs
```
