namespace Flight.API.Presentation.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Flight.API.Application.Commands;
using Flight.API.Application.DTOs;
using Flight.API.Application.Queries;

[ApiController]
[Route("api/[controller]")]
public class FlightsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(FlightBookingResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> BookFlight(
        [FromBody] CreateFlightBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFlightBookingCommand
        {
            UserId = request.UserId,
            DepartureCity = request.DepartureCity,
            ArrivalCity = request.ArrivalCity,
            DepartureDateUtc = request.DepartureDateUtc,
            ArrivalDateUtc = request.ArrivalDateUtc,
            Price = request.Price,
            PassengerCount = request.PassengerCount
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetFlightBooking), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FlightBookingResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFlightBooking(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetFlightBookingByIdQuery { FlightBookingId = id };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> ConfirmFlightBooking(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmFlightBookingCommand { FlightBookingId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelFlightBooking(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelFlightBookingCommand { FlightBookingId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
