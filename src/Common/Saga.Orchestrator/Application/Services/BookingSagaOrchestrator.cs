namespace Saga.Orchestrator.Application.Services;

using Microsoft.Extensions.Logging;
using Saga.Orchestrator.Application.Interfaces;
using Saga.Orchestrator.Domain.Models;

/// <summary>
/// Saga Orchestrator Service implementing the Saga Pattern for booking orchestration.
/// Manages distributed transactions across microservices.
/// </summary>
public class BookingSagaOrchestrator
{
    private readonly IBookingServiceClient _bookingClient;
    private readonly ILogger<BookingSagaOrchestrator> _logger;

    public BookingSagaOrchestrator(
        IBookingServiceClient bookingClient,
        ILogger<BookingSagaOrchestrator> logger
    )
    {
        _bookingClient = bookingClient;
        _logger = logger;
    }

    /// <summary>
    /// Execute the booking saga - orchestrates booking across multiple services.
    /// </summary>
    public async Task<BookingSagaState> ExecuteSagaAsync(
        BookingSagaState sagaState,
        FlightBookingRequest? flightRequest,
        HotelBookingRequest? hotelRequest,
        CarBookingRequest? carRequest,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation(
                "Starting booking saga for user {UserId} with booking ID {BookingId}",
                sagaState.UserId,
                sagaState.BookingId
            );

            // Step 1: Book Flight (if requested)
            if (sagaState.IncludeFlights && flightRequest != null)
            {
                sagaState = await BookFlightAsync(sagaState, flightRequest, cancellationToken);
                if (sagaState.FlightBookingStatus == StepStatus.Failed)
                {
                    throw new InvalidOperationException("Failed to book flight");
                }
            }

            // Step 2: Book Hotel (if requested)
            if (sagaState.IncludeHotel && hotelRequest != null)
            {
                sagaState = await BookHotelAsync(sagaState, hotelRequest, cancellationToken);
                if (sagaState.HotelBookingStatus == StepStatus.Failed)
                {
                    throw new InvalidOperationException("Failed to book hotel");
                }
            }

            // Step 3: Book Car (if requested)
            if (sagaState.IncludeCar && carRequest != null)
            {
                sagaState = await BookCarAsync(sagaState, carRequest, cancellationToken);
                if (sagaState.CarBookingStatus == StepStatus.Failed)
                {
                    throw new InvalidOperationException("Failed to book car");
                }
            }

            // All steps completed successfully
            sagaState.Status = SagaStatus.Completed;
            sagaState.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Booking saga completed successfully for user {UserId}",
                sagaState.UserId
            );

            return sagaState;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Booking saga failed for user {UserId}. Starting compensation.",
                sagaState.UserId
            );

            sagaState.FailureReason = ex.Message;
            sagaState = await CompensateSagaAsync(sagaState, cancellationToken);

            return sagaState;
        }
    }

    private async Task<BookingSagaState> BookFlightAsync(
        BookingSagaState sagaState,
        FlightBookingRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            sagaState.Status = SagaStatus.BookingFlights;
            sagaState.FlightBookingStatus = StepStatus.InProgress;

            var result = await _bookingClient.BookFlightAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                sagaState.FlightBookingStatus = StepStatus.Completed;
                sagaState.FlightBookingId = result.BookingId;
                sagaState.FlightConfirmationCode = result.ConfirmationCode;

                _logger.LogInformation(
                    "Flight booking successful: {ConfirmationCode}",
                    result.ConfirmationCode
                );
            }
            else
            {
                sagaState.FlightBookingStatus = StepStatus.Failed;
                _logger.LogWarning("Flight booking failed: {ErrorMessage}", result.ErrorMessage);
            }

            return sagaState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking flight");
            sagaState.FlightBookingStatus = StepStatus.Failed;
            throw;
        }
    }

    private async Task<BookingSagaState> BookHotelAsync(
        BookingSagaState sagaState,
        HotelBookingRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            sagaState.Status = SagaStatus.BookingHotel;
            sagaState.HotelBookingStatus = StepStatus.InProgress;

            var result = await _bookingClient.BookHotelAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                sagaState.HotelBookingStatus = StepStatus.Completed;
                sagaState.HotelBookingId = result.BookingId;
                sagaState.HotelConfirmationCode = result.ConfirmationCode;

                _logger.LogInformation(
                    "Hotel booking successful: {ConfirmationCode}",
                    result.ConfirmationCode
                );
            }
            else
            {
                sagaState.HotelBookingStatus = StepStatus.Failed;
                _logger.LogWarning("Hotel booking failed: {ErrorMessage}", result.ErrorMessage);
            }

            return sagaState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking hotel");
            sagaState.HotelBookingStatus = StepStatus.Failed;
            throw;
        }
    }

    private async Task<BookingSagaState> BookCarAsync(
        BookingSagaState sagaState,
        CarBookingRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            sagaState.Status = SagaStatus.BookingCar;
            sagaState.CarBookingStatus = StepStatus.InProgress;

            var result = await _bookingClient.BookCarAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                sagaState.CarBookingStatus = StepStatus.Completed;
                sagaState.CarBookingId = result.BookingId;
                sagaState.CarReservationCode = result.ReservationCode;

                _logger.LogInformation(
                    "Car booking successful: {ReservationCode}",
                    result.ReservationCode
                );
            }
            else
            {
                sagaState.CarBookingStatus = StepStatus.Failed;
                _logger.LogWarning("Car booking failed: {ErrorMessage}", result.ErrorMessage);
            }

            return sagaState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking car");
            sagaState.CarBookingStatus = StepStatus.Failed;
            throw;
        }
    }

    /// <summary>
    /// Compensate for failed booking saga steps.
    /// </summary>
    private async Task<BookingSagaState> CompensateSagaAsync(
        BookingSagaState sagaState,
        CancellationToken cancellationToken
    )
    {
        sagaState.Status = SagaStatus.Compensating;

        // Cancel in reverse order
        if (sagaState.CarBookingStatus == StepStatus.Completed && sagaState.CarBookingId.HasValue)
        {
            try
            {
                await _bookingClient.CancelCarAsync(
                    sagaState.CarBookingId.Value,
                    cancellationToken
                );
                sagaState.CarBookingStatus = StepStatus.Compensated;
                sagaState.CompensatedSteps.Add("Car");
                _logger.LogInformation("Car booking compensated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate car booking");
            }
        }

        if (
            sagaState.HotelBookingStatus == StepStatus.Completed
            && sagaState.HotelBookingId.HasValue
        )
        {
            try
            {
                await _bookingClient.CancelHotelAsync(
                    sagaState.HotelBookingId.Value,
                    cancellationToken
                );
                sagaState.HotelBookingStatus = StepStatus.Compensated;
                sagaState.CompensatedSteps.Add("Hotel");
                _logger.LogInformation("Hotel booking compensated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate hotel booking");
            }
        }

        if (
            sagaState.FlightBookingStatus == StepStatus.Completed
            && sagaState.FlightBookingId.HasValue
        )
        {
            try
            {
                await _bookingClient.CancelFlightAsync(
                    sagaState.FlightBookingId.Value,
                    cancellationToken
                );
                sagaState.FlightBookingStatus = StepStatus.Compensated;
                sagaState.CompensatedSteps.Add("Flight");
                _logger.LogInformation("Flight booking compensated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compensate flight booking");
            }
        }

        sagaState.Status = SagaStatus.Failed;
        sagaState.CompletedAt = DateTime.UtcNow;

        return sagaState;
    }
}
