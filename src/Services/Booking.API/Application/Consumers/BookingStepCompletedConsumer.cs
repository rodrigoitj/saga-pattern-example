namespace Booking.API.Application.Consumers;

using System.Linq;
using Booking.API.Application.Observability;
using Booking.API.Domain.Entities;
using MassTransit;
using Shared.Domain.Abstractions;
using Shared.Domain.IntegrationEvents;

public class BookingStepCompletedConsumer : IConsumer<BookingStepCompletedIntegrationEvent>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly BookingMetrics _bookingMetrics;
    private readonly ILogger<BookingStepCompletedConsumer> _logger;

    public BookingStepCompletedConsumer(
        IRepository<Booking> bookingRepository,
        BookingMetrics bookingMetrics,
        ILogger<BookingStepCompletedConsumer> logger
    )
    {
        _bookingRepository = bookingRepository;
        _bookingMetrics = bookingMetrics;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingStepCompletedIntegrationEvent> context)
    {
        var message = context.Message;
        var booking = await _bookingRepository.GetByIdAsync(
            message.BookingId,
            context.CancellationToken
        );
        if (booking is null)
        {
            return;
        }

        var stepType = message.StepType switch
        {
            BookingStepType.Flight => SagaStepType.FlightBooking,
            BookingStepType.Hotel => SagaStepType.HotelBooking,
            BookingStepType.Car => SagaStepType.CarBooking,
            _ => SagaStepType.FlightBooking,
        };

        if (booking.Steps.Any(step => step.StepType == stepType))
        {
            return;
        }

        switch (message.StepType)
        {
            case BookingStepType.Flight:
                booking.AddFlightBooking(message.ExternalId, message.Price);
                break;
            case BookingStepType.Hotel:
                booking.AddHotelBooking(message.ExternalId, message.Price);
                break;
            case BookingStepType.Car:
                booking.AddCarBooking(message.ExternalId, message.Price);
                break;
        }

        _bookingMetrics.RecordStepCompleted(message.StepType.ToString().ToLowerInvariant());

        if (booking.IsReadyToConfirm())
        {
            booking.MarkAsConfirmed();
            _bookingMetrics.RecordBookingConfirmed();

            // Record the time taken for booking to reach completed status
            var durationSeconds = (DateTime.UtcNow - booking.CreatedAt).TotalSeconds;
            _bookingMetrics.RecordBookingCreationDuration(durationSeconds);
        }

        _logger.LogInformation(
            "Booking step completed. BookingId: {BookingId}, StepType: {StepType}, Confirmed: {Confirmed}",
            booking.Id,
            message.StepType,
            booking.Status == BookingStatus.Confirmed
        );

        await _bookingRepository.UpdateAsync(booking, context.CancellationToken);
        await _bookingRepository.SaveChangesAsync(context.CancellationToken);
    }
}
