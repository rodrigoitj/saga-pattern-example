namespace Flight.API.Application.Consumers;

using Flight.API.Domain.Entities;
using Flight.API.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.IntegrationEvents;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly FlightDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public BookingCreatedConsumer(FlightDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
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
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            flightBooking.Confirm();
            _dbContext.FlightBookings.Update(flightBooking);
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await _publishEndpoint.Publish(
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
        }
        catch (Exception ex)
        {
            // Mark as Failed in local database if entity was created
            // Note: In a real scenario, you might want to track the failed entity for auditing
            await _publishEndpoint.Publish(
                new BookingFailedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Flight,
                    Reason = ex.Message,
                },
                context.CancellationToken
            );
        }
    }
}
