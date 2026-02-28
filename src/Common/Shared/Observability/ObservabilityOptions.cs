namespace Shared.Infrastructure.Observability;

public sealed class ObservabilityOptions
{
    public string ServiceNamespace { get; set; } = "SagaPattern";
    public string ServiceVersion { get; set; } = "1.0.0";
    public bool EnableRuntimeMetrics { get; set; } = true;
    public bool EnableProcessMetrics { get; set; } = true;
    public ExporterOptions Exporters { get; set; } = new();
    public OtlpOptions Otlp { get; set; } = new();
    public SamplingOptions Sampling { get; set; } = new();
}

public sealed class ExporterOptions
{
    public bool Console { get; set; } = true;
    public bool Prometheus { get; set; } = false;
    public string PrometheusScrapeEndpointPath { get; set; } = "/metrics";
    public bool Otlp { get; set; } = false;
}

public sealed class OtlpOptions
{
    public string Endpoint { get; set; } = "http://otel-collector:4317";
    public string Protocol { get; set; } = "grpc";
    public string? Headers { get; set; }
}

public sealed class SamplingOptions
{
    public double TraceIdRatio { get; set; } = 1.0;
}
