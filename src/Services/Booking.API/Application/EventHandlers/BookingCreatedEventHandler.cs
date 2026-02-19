namespace Booking.API.Application.EventHandlers;

using Booking.API.Domain.Entities;
using Booking.API.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Saga.Orchestrator.Application.Services;
using Saga.Orchestrator.Domain.Models;
using Shared.Domain.Abstractions;

/// <summary>
/// Event handler for BookingCreatedEvent.
/// Triggers the Saga pattern orchestration for booking flights, hotels, and cars.
/// </summary>
public class BookingCreatedEventHandler : INotificationHandler<BookingCreatedEvent>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly BookingSagaOrchestrator _sagaOrchestrator;
    private readonly ILogger<BookingCreatedEventHandler> _logger;

    public BookingCreatedEventHandler(
        IRepository<Booking> bookingRepository,
        BookingSagaOrchestrator sagaOrchestrator,
        ILogger<BookingCreatedEventHandler> logger
    )
    {
        _bookingRepository = bookingRepository;
        _sagaOrchestrator = sagaOrchestrator;
        _logger = logger;
    }

    public async Task Handle(BookingCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Handling BookingCreatedEvent for booking {BookingId}",
                notification.BookingId
            );

            // Retrieve the booking
            var booking = await _bookingRepository.GetByIdAsync(
                notification.BookingId,
                cancellationToken
            );

            if (booking is null)
            {
                _logger.LogError("Booking {BookingId} not found", notification.BookingId);
                return;
            }

            // Create saga state
            var sagaState = new BookingSagaState
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                IncludeFlights = true, // TODO: Get from request/command
                IncludeHotel = true, // TODO: Get from request/command
                IncludeCar = true, // TODO: Get from request/command
            };

            // Create booking requests with demo data
            // In a production system, these would come from the initial booking request
            var flightRequest = CreateFlightBookingRequest(booking);
            var hotelRequest = CreateHotelBookingRequest(booking);
            var carRequest = CreateCarBookingRequest(booking);

            // Execute the saga
            var completedSagaState = await _sagaOrchestrator.ExecuteSagaAsync(
                sagaState,
                flightRequest,
                hotelRequest,
                carRequest,
                cancellationToken
            );

            // Update booking with saga results
            await UpdateBookingWithSagaResults(booking, completedSagaState, cancellationToken);

            _logger.LogInformation(
                "Booking saga completed with status {Status}",
                completedSagaState.Status
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling BookingCreatedEvent for booking {BookingId}",
                notification.BookingId
            );
            throw;
        }
    }

    private FlightBookingRequest CreateFlightBookingRequest(Booking booking)
    {
        return new FlightBookingRequest
        {
            UserId = booking.UserId,
            DepartureCity = "New York", // Demo data
            ArrivalCity = "Los Angeles", // Demo data
            DepartureDateUtc = booking.CheckInDate,
            ArrivalDateUtc = booking.CheckOutDate,
            Price = 500m, // Demo price
            PassengerCount = 1, // Demo value
        };
    }

    private HotelBookingRequest CreateHotelBookingRequest(Booking booking)
    {
        return new HotelBookingRequest
        {
            UserId = booking.UserId,
            HotelName = "Grand Hotel", // Demo data
            City = "Los Angeles", // Demo data
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            RoomCount = 1, // Demo value
            PricePerNight = 150m, // Demo price
        };
    }

    private CarBookingRequest CreateCarBookingRequest(Booking booking)
    {
        return new CarBookingRequest
        {
            UserId = booking.UserId,
            CarModel = "Toyota Camry", // Demo data
            Company = "Enterprise", // Demo data
            PickUpDate = booking.CheckInDate,
            ReturnDate = booking.CheckOutDate,
            PickUpLocation = "Los Angeles Airport", // Demo data
            PricePerDay = 50m, // Demo price
        };
    }

    private async Task UpdateBookingWithSagaResults(
        Booking booking,
        BookingSagaState sagaState,
        CancellationToken cancellationToken
    )
    {
        // Update booking with saga results
        if (
            sagaState.FlightBookingStatus == StepStatus.Completed
            && sagaState.FlightBookingId.HasValue
        )
        {
            booking.AddFlightBooking(sagaState.FlightBookingId.Value, 500m); // Use actual price from request
        }

        if (
            sagaState.HotelBookingStatus == StepStatus.Completed
            && sagaState.HotelBookingId.HasValue
        )
        {
            booking.AddHotelBooking(sagaState.HotelBookingId.Value, 750m); // Use actual price from request
        }

        if (sagaState.CarBookingStatus == StepStatus.Completed && sagaState.CarBookingId.HasValue)
        {
            booking.AddCarBooking(sagaState.CarBookingId.Value, 200m); // Use actual price from request
        }

        // Mark booking as confirmed if saga succeeded
        if (sagaState.IsSuccessful())
        {
            booking.MarkAsConfirmed();
        }
        else
        {
            booking.MarkAsFailed(sagaState.FailureReason ?? "Saga failed to complete");
        }

        // Save the updated booking
        await _bookingRepository.UpdateAsync(booking, cancellationToken);
        await _bookingRepository.SaveChangesAsync(cancellationToken);
    }
}
