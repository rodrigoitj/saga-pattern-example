namespace Car.API.Application.Queries;

using MediatR;
using Car.API.Application.DTOs;

public class GetCarRentalByIdQuery : IRequest<CarRentalResponseDto>
{
    public Guid CarRentalId { get; init; }
}
