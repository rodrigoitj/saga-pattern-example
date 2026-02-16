namespace Car.API.Application.Validators;

using FluentValidation;
using Car.API.Application.Commands;

public class CreateCarRentalCommandValidator : AbstractValidator<CreateCarRentalCommand>
{
    public CreateCarRentalCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("UserId is required");

        RuleFor(x => x.CarModel)
            .NotEmpty()
            .WithMessage("CarModel is required");

        RuleFor(x => x.Company)
            .NotEmpty()
            .WithMessage("Company is required");

        RuleFor(x => x.PricePerDay)
            .GreaterThan(0)
            .WithMessage("PricePerDay must be greater than 0");

        RuleFor(x => x.PickUpLocation)
            .NotEmpty()
            .WithMessage("PickUpLocation is required");

        RuleFor(x => x.ReturnDate)
            .GreaterThan(x => x.PickUpDate)
            .WithMessage("ReturnDate must be after PickUpDate");
    }
}
