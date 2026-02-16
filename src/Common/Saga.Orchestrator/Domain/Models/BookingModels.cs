namespace Saga.Orchestrator.Domain.Models;

/// <summary>
/// Booking request and response models for the Saga pattern.
/// </summary>

public class FlightBookingRequest
{
    public Guid UserId { get; init; }
    public string DepartureCity { get; init; } = string.Empty;
    public string ArrivalCity { get; init; } = string.Empty;
    public DateTime DepartureDateUtc { get; init; }
    public DateTime ArrivalDateUtc { get; init; }
    public decimal Price { get; init; }
    public int PassengerCount { get; init; }
}

public class FlightBookingResult
{
    public bool IsSuccess { get; init; }
    public Guid? BookingId { get; init; }
    public string? ConfirmationCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public class HotelBookingRequest
{
    public Guid UserId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public int RoomCount { get; init; }
    public decimal PricePerNight { get; init; }
}

public class HotelBookingResult
{
    public bool IsSuccess { get; init; }
    public Guid? BookingId { get; init; }
    public string? ConfirmationCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public class CarBookingRequest
{
    public Guid UserId { get; init; }
    public string CarModel { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public DateTime PickUpDate { get; init; }
    public DateTime ReturnDate { get; init; }
    public string PickUpLocation { get; init; } = string.Empty;
    public decimal PricePerDay { get; init; }
}

public class CarBookingResult
{
    public bool IsSuccess { get; init; }
    public Guid? BookingId { get; init; }
    public string? ReservationCode { get; init; }
    public string? ErrorMessage { get; init; }
}
