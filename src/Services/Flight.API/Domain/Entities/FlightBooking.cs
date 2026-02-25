namespace Flight.API.Domain.Entities;

using Shared.Domain.Abstractions;

public class FlightBooking : AggregateRoot
{
    public string ConfirmationCode { get; private set; } = string.Empty;
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public string DepartureCity { get; private set; } = string.Empty;
    public string ArrivalCity { get; private set; } = string.Empty;
    public DateTime DepartureDateUtc { get; private set; }
    public DateTime ArrivalDateUtc { get; private set; }
    public decimal Price { get; private set; }
    public FlightBookingStatus Status { get; private set; } = FlightBookingStatus.Pending;
    public int PassengerCount { get; private set; }

    public static FlightBooking Create(
        Guid bookingId,
        Guid userId,
        string departureCity,
        string arrivalCity,
        DateTime departureDate,
        DateTime arrivalDate,
        decimal price,
        int passengerCount = 1
    )
    {
        var flightBooking = new FlightBooking
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            UserId = userId,
            DepartureCity = departureCity,
            ArrivalCity = arrivalCity,
            DepartureDateUtc = departureDate,
            ArrivalDateUtc = arrivalDate,
            Price = price,
            PassengerCount = passengerCount,
            ConfirmationCode =
                $"FL{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString()[..6].ToUpper()}",
            Status = FlightBookingStatus.Pending,
        };
        return flightBooking;
    }

    public void Confirm()
    {
        if (Status != FlightBookingStatus.Pending)
            throw new InvalidOperationException(
                "Flight booking can only be confirmed from pending state"
            );

        Status = FlightBookingStatus.Confirmed;
    }

    public void Cancel()
    {
        Status = FlightBookingStatus.Cancelled;
    }
}

public enum FlightBookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Failed,
}
