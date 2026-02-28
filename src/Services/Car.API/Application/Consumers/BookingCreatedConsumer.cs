namespace Car.API.Application.Consumers;

using System.Diagnostics;
using Car.API.Application.Observability;
using Car.API.Domain.Entities;
using Car.API.Infrastructure.Persistence;
using MassTransit;
using Shared.Domain.IntegrationEvents;
using Shared.Infrastructure.Messaging.Outbox;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly CarDbContext _dbContext;
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly CarMetrics _carMetrics;

    public BookingCreatedConsumer(
        CarDbContext dbContext,
        IOutboxPublisher outboxPublisher,
        CarMetrics carMetrics
    )
    {
        _dbContext = dbContext;
        _outboxPublisher = outboxPublisher;
        _carMetrics = carMetrics;
    }

    public async Task Consume(ConsumeContext<BookingCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!message.IncludeCar)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var carRental = CarRental.Create(
                message.BookingId,
                message.UserId,
                "Toyota Camry",
                "Enterprise",
                message.CheckInDate,
                message.CheckOutDate,
                "Los Angeles Airport",
                50m
            );

            _dbContext.CarRentals.Add(carRental);
            carRental.Confirm();

            // Save the outbox message in the same transaction as the car rental
            await _outboxPublisher.PublishAsync(
                new BookingStepCompletedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Car,
                    ExternalId = carRental.Id,
                    Price = carRental.TotalPrice,
                    ConfirmationCode = carRental.ReservationCode,
                },
                context.CancellationToken
            );

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            stopwatch.Stop();
            _carMetrics.RecordRentalCreated();
            _carMetrics.RecordRentalConfirmed(carRental.TotalPrice);
            _carMetrics.RecordProcessingDuration(stopwatch.Elapsed.TotalSeconds, "confirmed");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _carMetrics.RecordRentalFailed(ex.GetType().Name);
            _carMetrics.RecordProcessingDuration(stopwatch.Elapsed.TotalSeconds, "failed");

            // Clear tracked entities from the failed operation
            _dbContext.ChangeTracker.Clear();

            // Save failure event to outbox — the outbox processor will publish it later
            await _outboxPublisher.PublishAsync(
                new BookingFailedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Car,
                    Reason = ex.Message,
                },
                context.CancellationToken
            );

            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}
