namespace Booking.API.Application.Validators;

using FluentValidation;
using Booking.API.Application.Commands;

/// <summary>
/// Validator for CreateBookingCommand.
/// Follows SOLID principles - Single Responsibility.
/// </summary>
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("UserId must not be empty");

        RuleFor(x => x.CheckInDate)
            .NotEmpty()
            .WithMessage("CheckInDate is required")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("CheckInDate must be in the future");

        RuleFor(x => x.CheckOutDate)
            .NotEmpty()
            .WithMessage("CheckOutDate is required")
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("CheckOutDate must be after CheckInDate");

        RuleFor(x => x)
            .Must(x => x.IncludeFlights || x.IncludeHotel || x.IncludeCar)
            .WithMessage("At least one booking option (flights, hotel, or car) must be selected");
    }
}
