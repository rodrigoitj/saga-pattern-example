using System.Diagnostics.Metrics;
using Shared.Infrastructure.Observability;

namespace Flight.API.Application.Observability;

/// <summary>
/// Custom metrics for the Flight service following OpenTelemetry + Prometheus conventions.
/// Covers RED method (Rate, Errors, Duration) and business KPIs.
/// </summary>
public sealed class FlightMetrics
{
    private readonly Counter<long> _reservationsCreatedTotal;
    private readonly Counter<long> _reservationsConfirmedTotal;
    private readonly Counter<long> _reservationsCancelledTotal;
    private readonly Counter<long> _reservationsFailedTotal;
    private readonly Histogram<double> _reservationProcessingDurationSeconds;
    private readonly Counter<double> _revenueTotal;
    private readonly UpDownCounter<long> _activeReservations;

    public FlightMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(TelemetryConstants.FlightMeterName);

        _reservationsCreatedTotal = meter.CreateCounter<long>(
            "flight.reservations.created.total",
            description: "Total number of flight reservations created"
        );

        _reservationsConfirmedTotal = meter.CreateCounter<long>(
            "flight.reservations.confirmed.total",
            description: "Total number of flight reservations confirmed"
        );

        _reservationsCancelledTotal = meter.CreateCounter<long>(
            "flight.reservations.cancelled.total",
            description: "Total number of flight reservations cancelled (compensated)"
        );

        _reservationsFailedTotal = meter.CreateCounter<long>(
            "flight.reservations.failed.total",
            description: "Total number of flight reservation failures"
        );

        _reservationProcessingDurationSeconds = meter.CreateHistogram<double>(
            "flight.reservation.processing.duration.seconds",
            unit: "s",
            description: "Time taken to process a flight reservation"
        );

        _revenueTotal = meter.CreateCounter<double>(
            "flight.revenue.total",
            unit: "{USD}",
            description: "Total revenue from flight reservations"
        );

        _activeReservations = meter.CreateUpDownCounter<long>(
            "flight.reservations.active",
            description: "Number of currently active (non-cancelled/non-failed) flight reservations"
        );
    }

    public void RecordReservationCreated()
    {
        _reservationsCreatedTotal.Add(1);
        _activeReservations.Add(1);
    }

    public void RecordReservationConfirmed(decimal price)
    {
        _reservationsConfirmedTotal.Add(1);
        _revenueTotal.Add((double)price);
    }

    public void RecordReservationCancelled()
    {
        _reservationsCancelledTotal.Add(1);
        _activeReservations.Add(-1);
    }

    public void RecordReservationFailed(string reason)
    {
        _reservationsFailedTotal.Add(
            1,
            new KeyValuePair<string, object?>("failure.reason", reason)
        );
        _activeReservations.Add(-1);
    }

    public void RecordProcessingDuration(double durationSeconds, string status)
    {
        _reservationProcessingDurationSeconds.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("status", status)
        );
    }
}
