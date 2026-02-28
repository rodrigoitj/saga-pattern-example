namespace Hotel.API.Application.Consumers;

using System.Diagnostics;
using Hotel.API.Application.Observability;
using Hotel.API.Domain.Entities;
using Hotel.API.Infrastructure.Persistence;
using MassTransit;
using Shared.Domain.IntegrationEvents;
using Shared.Infrastructure.Messaging.Outbox;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly HotelDbContext _dbContext;
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly HotelMetrics _hotelMetrics;

    public BookingCreatedConsumer(
        HotelDbContext dbContext,
        IOutboxPublisher outboxPublisher,
        HotelMetrics hotelMetrics
    )
    {
        _dbContext = dbContext;
        _outboxPublisher = outboxPublisher;
        _hotelMetrics = hotelMetrics;
    }

    public async Task Consume(ConsumeContext<BookingCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!message.IncludeHotel)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

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

            stopwatch.Stop();
            _hotelMetrics.RecordReservationCreated();
            _hotelMetrics.RecordReservationConfirmed(hotelBooking.TotalPrice);
            _hotelMetrics.RecordProcessingDuration(stopwatch.Elapsed.TotalSeconds, "confirmed");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _hotelMetrics.RecordReservationFailed(ex.GetType().Name);
            _hotelMetrics.RecordProcessingDuration(stopwatch.Elapsed.TotalSeconds, "failed");

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
