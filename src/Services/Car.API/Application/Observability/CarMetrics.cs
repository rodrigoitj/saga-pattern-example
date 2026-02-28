using System.Diagnostics.Metrics;
using Shared.Infrastructure.Observability;

namespace Car.API.Application.Observability;

/// <summary>
/// Custom metrics for the Car service following OpenTelemetry + Prometheus conventions.
/// Covers RED method (Rate, Errors, Duration) and business KPIs.
/// </summary>
public sealed class CarMetrics
{
    private readonly Counter<long> _rentalsCreatedTotal;
    private readonly Counter<long> _rentalsConfirmedTotal;
    private readonly Counter<long> _rentalsCancelledTotal;
    private readonly Counter<long> _rentalsFailedTotal;
    private readonly Histogram<double> _rentalProcessingDurationSeconds;
    private readonly Counter<double> _revenueTotal;
    private readonly UpDownCounter<long> _activeRentals;

    public CarMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(TelemetryConstants.CarMeterName);

        _rentalsCreatedTotal = meter.CreateCounter<long>(
            "car.rentals.created.total",
            description: "Total number of car rentals created"
        );

        _rentalsConfirmedTotal = meter.CreateCounter<long>(
            "car.rentals.confirmed.total",
            description: "Total number of car rentals confirmed"
        );

        _rentalsCancelledTotal = meter.CreateCounter<long>(
            "car.rentals.cancelled.total",
            description: "Total number of car rentals cancelled (compensated)"
        );

        _rentalsFailedTotal = meter.CreateCounter<long>(
            "car.rentals.failed.total",
            description: "Total number of car rental failures"
        );

        _rentalProcessingDurationSeconds = meter.CreateHistogram<double>(
            "car.rental.processing.duration.seconds",
            unit: "s",
            description: "Time taken to process a car rental"
        );

        _revenueTotal = meter.CreateCounter<double>(
            "car.revenue.total",
            unit: "{USD}",
            description: "Total revenue from car rentals"
        );

        _activeRentals = meter.CreateUpDownCounter<long>(
            "car.rentals.active",
            description: "Number of currently active (non-cancelled/non-failed) car rentals"
        );
    }

    public void RecordRentalCreated()
    {
        _rentalsCreatedTotal.Add(1);
        _activeRentals.Add(1);
    }

    public void RecordRentalConfirmed(decimal price)
    {
        _rentalsConfirmedTotal.Add(1);
        _revenueTotal.Add((double)price);
    }

    public void RecordRentalCancelled()
    {
        _rentalsCancelledTotal.Add(1);
        _activeRentals.Add(-1);
    }

    public void RecordRentalFailed(string reason)
    {
        _rentalsFailedTotal.Add(1, new KeyValuePair<string, object?>("failure.reason", reason));
        _activeRentals.Add(-1);
    }

    public void RecordProcessingDuration(double durationSeconds, string status)
    {
        _rentalProcessingDurationSeconds.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("status", status)
        );
    }
}
