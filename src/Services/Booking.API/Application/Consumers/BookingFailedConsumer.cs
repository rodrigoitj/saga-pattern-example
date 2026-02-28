namespace Booking.API.Application.Consumers;

using Booking.API.Application.Observability;
using Booking.API.Domain.Entities;
using MassTransit;
using Shared.Domain.Abstractions;
using Shared.Domain.IntegrationEvents;

public class BookingFailedConsumer : IConsumer<BookingFailedIntegrationEvent>
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly BookingMetrics _bookingMetrics;
    private readonly ILogger<BookingFailedConsumer> _logger;

    public BookingFailedConsumer(
        IRepository<Booking> bookingRepository,
        BookingMetrics bookingMetrics,
        ILogger<BookingFailedConsumer> logger
    )
    {
        _bookingRepository = bookingRepository;
        _bookingMetrics = bookingMetrics;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingFailedIntegrationEvent> context)
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

        booking.MarkStepFailed(stepType, message.Reason);
        booking.MarkAsFailed(message.Reason);

        _bookingMetrics.RecordBookingFailed(message.StepType.ToString().ToLowerInvariant());
        _logger.LogWarning(
            "Booking failed. BookingId: {BookingId}, StepType: {StepType}, Reason: {Reason}",
            booking.Id,
            message.StepType,
            message.Reason
        );

        await _bookingRepository.UpdateAsync(booking, context.CancellationToken);
        await _bookingRepository.SaveChangesAsync(context.CancellationToken);
    }
}
