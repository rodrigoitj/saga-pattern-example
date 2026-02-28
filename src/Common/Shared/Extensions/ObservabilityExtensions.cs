using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Observability;

namespace Shared.Infrastructure.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string serviceName
    )
    {
        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection("Observability")
        );
        builder.Services.AddHttpClient();

        var observability =
            builder.Configuration.GetSection("Observability").Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        var serviceVersion = ResolveServiceVersion() ?? observability.ServiceVersion;
        var sourceName = $"{TelemetryConstants.ActivitySourcePrefix}.{serviceName}";

        builder.Services.AddSingleton(new ActivitySource(sourceName));
        builder.Services.AddSingleton<MessagingMetrics>();

        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource
                    .AddService(
                        serviceName: serviceName,
                        serviceNamespace: observability.ServiceNamespace,
                        serviceVersion: serviceVersion
                    )
                    .AddAttributes(
                        new[]
                        {
                            new KeyValuePair<string, object>(
                                "deployment.environment",
                                builder.Environment.EnvironmentName
                            ),
                        }
                    );
            })
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(
                        new ParentBasedSampler(
                            new TraceIdRatioBasedSampler(
                                Math.Clamp(observability.Sampling.TraceIdRatio, 0.0, 1.0)
                            )
                        )
                    )
                    .AddSource(sourceName)
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForStoredProcedure = false;
                        options.SetDbStatementForText = false;
                    });

                ConfigureTracingExporters(tracing, observability);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(TelemetryConstants.MessagingMeterName)
                    .AddMeter(TelemetryConstants.BookingMeterName)
                    .AddMeter(TelemetryConstants.FlightMeterName)
                    .AddMeter(TelemetryConstants.HotelMeterName)
                    .AddMeter(TelemetryConstants.CarMeterName)
                    .AddView(
                        instrumentName: "http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = [5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000],
                        }
                    );

                if (observability.EnableRuntimeMetrics)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                if (observability.EnableProcessMetrics)
                {
                    metrics.AddProcessInstrumentation();
                }

                ConfigureMetricExporters(metrics, observability);
            });

        ConfigureSerilog(builder, serviceName, serviceVersion, observability);

        return builder;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        var observability =
            app.Configuration.GetSection("Observability").Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        app.UseMiddleware<CorrelationIdMiddleware>();

        if (observability.Exporters.Prometheus)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint(
                observability.Exporters.PrometheusScrapeEndpointPath
            );
        }

        return app;
    }

    private static void ConfigureTracingExporters(
        TracerProviderBuilder tracing,
        ObservabilityOptions observability
    )
    {
        if (observability.Exporters.Console)
        {
            tracing.AddConsoleExporter();
        }

        if (observability.Exporters.Otlp)
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(observability.Otlp.Endpoint);
                options.Protocol = observability.Otlp.Protocol.Equals(
                    "http/protobuf",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf
                    : OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

                if (!string.IsNullOrWhiteSpace(observability.Otlp.Headers))
                {
                    options.Headers = observability.Otlp.Headers;
                }
            });
        }
    }

    private static void ConfigureMetricExporters(
        MeterProviderBuilder metrics,
        ObservabilityOptions observability
    )
    {
        if (observability.Exporters.Console)
        {
            metrics.AddConsoleExporter();
        }

        if (observability.Exporters.Prometheus)
        {
            metrics.AddPrometheusExporter();
        }

        if (observability.Exporters.Otlp)
        {
            metrics.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(observability.Otlp.Endpoint);
                options.Protocol = observability.Otlp.Protocol.Equals(
                    "http/protobuf",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf
                    : OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;

                if (!string.IsNullOrWhiteSpace(observability.Otlp.Headers))
                {
                    options.Headers = observability.Otlp.Headers;
                }
            });
        }
    }

    private static void ConfigureSerilog(
        WebApplicationBuilder builder,
        string serviceName,
        string serviceVersion,
        ObservabilityOptions observability
    )
    {
        builder.Host.UseSerilog(
            (_, _, loggerConfiguration) =>
            {
                loggerConfiguration
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.With<ActivityTraceEnricher>()
                    .Enrich.WithProperty("service.name", serviceName)
                    .Enrich.WithProperty("service.version", serviceVersion);

                if (observability.Exporters.Console)
                {
                    loggerConfiguration.WriteTo.Console();
                }

                if (observability.Exporters.Otlp)
                {
                    loggerConfiguration.WriteTo.OpenTelemetry(options =>
                    {
                        options.Endpoint = observability.Otlp.Endpoint;
                        options.Protocol = observability.Otlp.Protocol.Equals(
                            "http/protobuf",
                            StringComparison.OrdinalIgnoreCase
                        )
                            ? Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf
                            : Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                        options.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = serviceName,
                            ["service.version"] = serviceVersion,
                            ["deployment.environment"] = builder.Environment.EnvironmentName,
                        };

                        if (!string.IsNullOrWhiteSpace(observability.Otlp.Headers))
                        {
                            options.Headers = observability
                                .Otlp.Headers.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(header =>
                                    header.Split('=', 2, StringSplitOptions.TrimEntries)
                                )
                                .Where(parts => parts.Length == 2)
                                .ToDictionary(parts => parts[0], parts => parts[1]);
                        }
                    });
                }
            }
        );
    }

    private static string? ResolveServiceVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
}
