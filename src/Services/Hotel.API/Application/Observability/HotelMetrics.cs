using System.Diagnostics.Metrics;
using Shared.Infrastructure.Observability;

namespace Hotel.API.Application.Observability;

/// <summary>
/// Custom metrics for the Hotel service following OpenTelemetry + Prometheus conventions.
/// Covers RED method (Rate, Errors, Duration) and business KPIs.
/// </summary>
public sealed class HotelMetrics
{
    private readonly Counter<long> _reservationsCreatedTotal;
    private readonly Counter<long> _reservationsConfirmedTotal;
    private readonly Counter<long> _reservationsCancelledTotal;
    private readonly Counter<long> _reservationsFailedTotal;
    private readonly Histogram<double> _reservationProcessingDurationSeconds;
    private readonly Counter<double> _revenueTotal;
    private readonly UpDownCounter<long> _activeReservations;

    public HotelMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(TelemetryConstants.HotelMeterName);

        _reservationsCreatedTotal = meter.CreateCounter<long>(
            "hotel.reservations.created.total",
            description: "Total number of hotel reservations created"
        );

        _reservationsConfirmedTotal = meter.CreateCounter<long>(
            "hotel.reservations.confirmed.total",
            description: "Total number of hotel reservations confirmed"
        );

        _reservationsCancelledTotal = meter.CreateCounter<long>(
            "hotel.reservations.cancelled.total",
            description: "Total number of hotel reservations cancelled (compensated)"
        );

        _reservationsFailedTotal = meter.CreateCounter<long>(
            "hotel.reservations.failed.total",
            description: "Total number of hotel reservation failures"
        );

        _reservationProcessingDurationSeconds = meter.CreateHistogram<double>(
            "hotel.reservation.processing.duration.seconds",
            unit: "s",
            description: "Time taken to process a hotel reservation"
        );

        _revenueTotal = meter.CreateCounter<double>(
            "hotel.revenue.total",
            unit: "{USD}",
            description: "Total revenue from hotel reservations"
        );

        _activeReservations = meter.CreateUpDownCounter<long>(
            "hotel.reservations.active",
            description: "Number of currently active (non-cancelled/non-failed) hotel reservations"
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
