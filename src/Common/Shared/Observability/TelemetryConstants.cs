namespace Shared.Infrastructure.Observability;

public static class TelemetryConstants
{
    public const string ActivitySourcePrefix = "SagaPattern";
    public const string MessagingMeterName = "SagaPattern.Messaging";
    public const string BookingMeterName = "SagaPattern.Booking";
    public const string FlightMeterName = "SagaPattern.Flight";
    public const string HotelMeterName = "SagaPattern.Hotel";
    public const string CarMeterName = "SagaPattern.Car";
}
