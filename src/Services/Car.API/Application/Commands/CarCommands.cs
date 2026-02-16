namespace Car.API.Application.Commands;

using MediatR;
using Car.API.Application.DTOs;

public class CreateCarRentalCommand : IRequest<CarRentalResponseDto>
{
    public Guid UserId { get; init; }
    public string CarModel { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public DateTime PickUpDate { get; init; }
    public DateTime ReturnDate { get; init; }
    public string PickUpLocation { get; init; } = string.Empty;
    public decimal PricePerDay { get; init; }
}

public class ConfirmCarRentalCommand : IRequest<CarRentalResponseDto>
{
    public Guid CarRentalId { get; init; }
}

public class CancelCarRentalCommand : IRequest<CarRentalResponseDto>
{
    public Guid CarRentalId { get; init; }
}
