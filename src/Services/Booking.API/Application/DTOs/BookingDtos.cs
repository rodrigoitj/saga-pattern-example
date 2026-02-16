namespace Booking.API.Application.DTOs;

public class CreateBookingRequestDto
{
    public Guid UserId { get; init; }
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public bool IncludeFlights { get; init; }
    public bool IncludeHotel { get; init; }
    public bool IncludeCar { get; init; }
}

public class BookingResponseDto
{
    public Guid Id { get; init; }
    public string ReferenceNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalPrice { get; init; }
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public Guid? FlightBookingId { get; init; }
    public Guid? HotelBookingId { get; init; }
    public Guid? CarBookingId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class BookingStepDto
{
    public int Id { get; init; }
    public string StepType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid ExternalId { get; init; }
}
