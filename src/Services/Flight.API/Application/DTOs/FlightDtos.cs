namespace Flight.API.Application.DTOs;

public class CreateFlightBookingRequestDto
{
    public Guid UserId { get; init; }
    public string DepartureCity { get; init; } = string.Empty;
    public string ArrivalCity { get; init; } = string.Empty;
    public DateTime DepartureDateUtc { get; init; }
    public DateTime ArrivalDateUtc { get; init; }
    public decimal Price { get; init; }
    public int PassengerCount { get; init; } = 1;
}

public class FlightBookingResponseDto
{
    public Guid Id { get; init; }
    public string ConfirmationCode { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string DepartureCity { get; init; } = string.Empty;
    public string ArrivalCity { get; init; } = string.Empty;
    public DateTime DepartureDateUtc { get; init; }
    public DateTime ArrivalDateUtc { get; init; }
    public decimal Price { get; init; }
    public string Status { get; init; } = string.Empty;
    public int PassengerCount { get; init; }
}
