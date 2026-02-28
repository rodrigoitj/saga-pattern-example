using System.Diagnostics.Metrics;
using Shared.Infrastructure.Observability;

namespace Booking.API.Application.Observability;

public sealed class BookingMetrics
{
    private readonly Counter<long> _bookingCreatedTotal;
    private readonly Counter<long> _bookingConfirmedTotal;
    private readonly Counter<long> _bookingFailedTotal;
    private readonly Counter<long> _bookingStepCompletedTotal;
    private readonly Counter<long> _bookingCancelledTotal;
    private readonly Histogram<double> _bookingCreationDurationSeconds;
    private readonly Counter<double> _revenueTotal;
    private readonly UpDownCounter<long> _bookingsInFlight;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(TelemetryConstants.BookingMeterName);

        _bookingCreatedTotal = meter.CreateCounter<long>(
            "booking.created.total",
            description: "Total number of bookings created"
        );
        _bookingConfirmedTotal = meter.CreateCounter<long>(
            "booking.confirmed.total",
            description: "Total number of bookings confirmed successfully"
        );
        _bookingFailedTotal = meter.CreateCounter<long>(
            "booking.failed.total",
            description: "Total number of bookings that failed"
        );
        _bookingStepCompletedTotal = meter.CreateCounter<long>(
            "booking.step.completed.total",
            description: "Total number of saga steps completed"
        );
        _bookingCancelledTotal = meter.CreateCounter<long>(
            "booking.cancelled.total",
            description: "Total number of bookings cancelled by user"
        );
        _bookingCreationDurationSeconds = meter.CreateHistogram<double>(
            "booking.creation.duration.seconds",
            "s",
            "Time taken for a booking to reach completed status"
        );
        _revenueTotal = meter.CreateCounter<double>(
            "booking.revenue.total",
            unit: "{USD}",
            description: "Total revenue from confirmed bookings"
        );
        _bookingsInFlight = meter.CreateUpDownCounter<long>(
            "booking.inflight",
            description: "Number of bookings currently being processed (not yet confirmed or failed)"
        );
    }

    public void RecordBookingCreated(bool includeFlights, bool includeHotel, bool includeCar)
    {
        _bookingCreatedTotal.Add(
            1,
            new KeyValuePair<string, object?>("flight", includeFlights),
            new KeyValuePair<string, object?>("hotel", includeHotel),
            new KeyValuePair<string, object?>("car", includeCar)
        );
        _bookingsInFlight.Add(1);
    }

    public void RecordStepCompleted(string stepType)
    {
        _bookingStepCompletedTotal.Add(1, new KeyValuePair<string, object?>("step", stepType));
    }

    public void RecordBookingConfirmed()
    {
        _bookingConfirmedTotal.Add(1);
        _bookingsInFlight.Add(-1);
    }

    public void RecordBookingConfirmed(decimal totalPrice)
    {
        _bookingConfirmedTotal.Add(1);
        _bookingsInFlight.Add(-1);
        _revenueTotal.Add((double)totalPrice);
    }

    public void RecordBookingFailed(string stepType)
    {
        _bookingFailedTotal.Add(1, new KeyValuePair<string, object?>("step", stepType));
        _bookingsInFlight.Add(-1);
    }

    public void RecordBookingCancelled()
    {
        _bookingCancelledTotal.Add(1);
    }

    public void RecordBookingCreationDuration(double durationSeconds, string status = "completed")
    {
        _bookingCreationDurationSeconds.Record(
            durationSeconds,
            new KeyValuePair<string, object?>("status", status)
        );
    }
}
