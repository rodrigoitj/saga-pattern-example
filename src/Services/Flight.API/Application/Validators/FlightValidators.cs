namespace Flight.API.Application.Validators;

using FluentValidation;
using Flight.API.Application.Commands;

public class CreateFlightBookingCommandValidator : AbstractValidator<CreateFlightBookingCommand>
{
    public CreateFlightBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("UserId is required");

        RuleFor(x => x.DepartureCity)
            .NotEmpty()
            .WithMessage("DepartureCity is required");

        RuleFor(x => x.ArrivalCity)
            .NotEmpty()
            .WithMessage("ArrivalCity is required");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0");

        RuleFor(x => x.PassengerCount)
            .GreaterThan(0)
            .WithMessage("PassengerCount must be greater than 0");

        RuleFor(x => x.DepartureDateUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("DepartureDateUtc must be in the future");
    }
}
