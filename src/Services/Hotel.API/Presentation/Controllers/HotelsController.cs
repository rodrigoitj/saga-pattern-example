namespace Hotel.API.Presentation.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Hotel.API.Application.Commands;
using Hotel.API.Application.DTOs;
using Hotel.API.Application.Queries;

[ApiController]
[Route("api/[controller]")]
public class HotelsController : ControllerBase
{
    private readonly IMediator _mediator;

    public HotelsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> BookHotel(
        [FromBody] CreateHotelBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateHotelBookingCommand
        {
            UserId = request.UserId,
            HotelName = request.HotelName,
            City = request.City,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            RoomCount = request.RoomCount,
            PricePerNight = request.PricePerNight
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetHotelBooking), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHotelBooking(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetHotelBookingByIdQuery { HotelBookingId = id };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> ConfirmHotelBooking(Guid id, CancellationToken cancellationToken)
    {
        var command = new ConfirmHotelBookingCommand { HotelBookingId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelHotelBooking(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelHotelBookingCommand { HotelBookingId = id };
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
