namespace Hotel.API.Application.DTOs;

public class CreateHotelBookingRequestDto
{
    public Guid UserId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public int RoomCount { get; init; }
    public decimal PricePerNight { get; init; }
}

public class HotelBookingResponseDto
{
    public Guid Id { get; init; }
    public string ConfirmationCode { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public int RoomCount { get; init; }
    public decimal PricePerNight { get; init; }
    public decimal TotalPrice { get; init; }
    public string Status { get; init; } = string.Empty;
}
