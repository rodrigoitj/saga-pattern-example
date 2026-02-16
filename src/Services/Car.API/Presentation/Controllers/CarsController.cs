namespace Car.API.Presentation.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Car.API.Application.Commands;
using Car.API.Application.DTOs;
using Car.API.Application.Queries;

[ApiController]
[Route("api/[controller]")]
public class CarsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CarsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> RentCar(
        [FromBody] CreateCarRentalRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCarRentalCommand
        {
            UserId = request.UserId,
            CarModel = request.CarModel,
            Company = request.Company,
            PickUpDate = request.PickUpDate,
            ReturnDate = request.ReturnDate,
            PickUpLocation = request.PickUpLocation,
            PricePerDay = request.PricePerDay
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCarRental), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCarRental(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetCarRentalByIdQuery { CarRentalId = id };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> ConfirmCarRental(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmCarRentalCommand { CarRentalId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelCarRental(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelCarRentalCommand { CarRentalId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
