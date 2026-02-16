namespace Booking.API.Presentation.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Booking.API.Application.Commands;
using Booking.API.Application.DTOs;
using Booking.API.Application.Queries;
using FluentValidation;

/// <summary>
/// Booking API Controller.
/// Implements Clean Architecture - Presentation layer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IMediator mediator, ILogger<BookingsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new booking.
    /// </summary>
    /// <param name="request">Booking creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created booking</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateBookingCommand
            {
                UserId = request.UserId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                IncludeFlights = request.IncludeFlights,
                IncludeHotel = request.IncludeHotel,
                IncludeCar = request.IncludeCar
            };

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetBooking), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating booking");
            return BadRequest(new { errors = ex.Message });
        }
    }

    /// <summary>
    /// Get a booking by ID.
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Booking details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookingResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetBookingByIdQuery { BookingId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Get all bookings for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of bookings</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<BookingResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBookings(Guid userId, CancellationToken cancellationToken)
    {
        var query = new GetUserBookingsQuery { UserId = userId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
