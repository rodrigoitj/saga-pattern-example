namespace Flight.API.Application.Consumers;

using Flight.API.Domain.Entities;
using Flight.API.Infrastructure.Persistence;
using MassTransit;
using Shared.Domain.IntegrationEvents;
using Shared.Infrastructure.Messaging.Outbox;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly FlightDbContext _dbContext;
    private readonly IOutboxPublisher _outboxPublisher;

    public BookingCreatedConsumer(FlightDbContext dbContext, IOutboxPublisher outboxPublisher)
    {
        _dbContext = dbContext;
        _outboxPublisher = outboxPublisher;
    }

    public async Task Consume(ConsumeContext<BookingCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!message.IncludeFlights)
        {
            return;
        }

        try
        {
            var flightBooking = FlightBooking.Create(
                message.BookingId,
                message.UserId,
                "New York",
                "Los Angeles",
                message.CheckInDate,
                message.CheckOutDate,
                500m,
                1
            );

            _dbContext.FlightBookings.Add(flightBooking);
            flightBooking.Confirm();

            // Save the outbox message in the same transaction as the flight booking
            await _outboxPublisher.PublishAsync(
                new BookingStepCompletedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Flight,
                    ExternalId = flightBooking.Id,
                    Price = flightBooking.Price,
                    ConfirmationCode = flightBooking.ConfirmationCode,
                },
                context.CancellationToken
            );

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            // Clear tracked entities from the failed operation
            _dbContext.ChangeTracker.Clear();

            // Save failure event to outbox — the outbox processor will publish it later
            await _outboxPublisher.PublishAsync(
                new BookingFailedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Flight,
                    Reason = ex.Message,
                },
                context.CancellationToken
            );

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}
