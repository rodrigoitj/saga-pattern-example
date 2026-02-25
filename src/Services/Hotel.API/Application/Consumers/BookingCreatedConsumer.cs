namespace Hotel.API.Application.Consumers;

using Hotel.API.Domain.Entities;
using Hotel.API.Infrastructure.Persistence;
using MassTransit;
using Shared.Domain.IntegrationEvents;
using Shared.Infrastructure.Messaging.Outbox;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly HotelDbContext _dbContext;
    private readonly IOutboxPublisher _outboxPublisher;

    public BookingCreatedConsumer(HotelDbContext dbContext, IOutboxPublisher outboxPublisher)
    {
        _dbContext = dbContext;
        _outboxPublisher = outboxPublisher;
    }

    public async Task Consume(ConsumeContext<BookingCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!message.IncludeHotel)
        {
            return;
        }

        try
        {
            var hotelBooking = HotelBooking.Create(
                message.BookingId,
                message.UserId,
                "Grand Hotel",
                "Los Angeles",
                message.CheckInDate,
                message.CheckOutDate,
                1,
                150m
            );

            _dbContext.HotelBookings.Add(hotelBooking);
            hotelBooking.Confirm();

            // Save the outbox message in the same transaction as the hotel booking
            await _outboxPublisher.PublishAsync(
                new BookingStepCompletedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Hotel,
                    ExternalId = hotelBooking.Id,
                    Price = hotelBooking.TotalPrice,
                    ConfirmationCode = hotelBooking.ConfirmationCode,
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
                    StepType = BookingStepType.Hotel,
                    Reason = ex.Message,
                },
                context.CancellationToken
            );

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}
