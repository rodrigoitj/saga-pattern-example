namespace Booking.API.Application.Consumers;

using Booking.API.Domain.Entities;
using MassTransit;
using Shared.Domain.Abstractions;
using Shared.Domain.IntegrationEvents;

public class BookingFailedConsumer : IConsumer<BookingFailedIntegrationEvent>
{
    private readonly IRepository<Booking> _bookingRepository;

    public BookingFailedConsumer(IRepository<Booking> bookingRepository)
    {
        _bookingRepository = bookingRepository;
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

        await _bookingRepository.UpdateAsync(booking, context.CancellationToken);
        await _bookingRepository.SaveChangesAsync(context.CancellationToken);
    }
}
