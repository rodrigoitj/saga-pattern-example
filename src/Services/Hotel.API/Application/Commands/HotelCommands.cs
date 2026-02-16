namespace Hotel.API.Application.Commands;

using MediatR;
using Hotel.API.Application.DTOs;

public class CreateHotelBookingCommand : IRequest<HotelBookingResponseDto>
{
    public Guid UserId { get; init; }
    public string HotelName { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public DateTime CheckInDate { get; init; }
    public DateTime CheckOutDate { get; init; }
    public int RoomCount { get; init; }
    public decimal PricePerNight { get; init; }
}

public class ConfirmHotelBookingCommand : IRequest<HotelBookingResponseDto>
{
    public Guid HotelBookingId { get; init; }
}

public class CancelHotelBookingCommand : IRequest<HotelBookingResponseDto>
{
    public Guid HotelBookingId { get; init; }
}
