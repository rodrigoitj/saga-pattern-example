namespace Hotel.API.Application.Validators;

using FluentValidation;
using Hotel.API.Application.Commands;

public class CreateHotelBookingCommandValidator : AbstractValidator<CreateHotelBookingCommand>
{
    public CreateHotelBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("UserId is required");

        RuleFor(x => x.HotelName)
            .NotEmpty()
            .WithMessage("HotelName is required");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required");

        RuleFor(x => x.PricePerNight)
            .GreaterThan(0)
            .WithMessage("PricePerNight must be greater than 0");

        RuleFor(x => x.RoomCount)
            .GreaterThan(0)
            .WithMessage("RoomCount must be greater than 0");

        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("CheckOutDate must be after CheckInDate");
    }
}
