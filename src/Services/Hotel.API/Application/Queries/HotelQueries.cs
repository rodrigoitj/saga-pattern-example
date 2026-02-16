namespace Hotel.API.Application.Queries;

using MediatR;
using Hotel.API.Application.DTOs;

public class GetHotelBookingByIdQuery : IRequest<HotelBookingResponseDto>
{
    public Guid HotelBookingId { get; init; }
}
