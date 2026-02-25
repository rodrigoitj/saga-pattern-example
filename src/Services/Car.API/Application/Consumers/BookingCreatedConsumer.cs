namespace Car.API.Application.Consumers;

using Car.API.Domain.Entities;
using Car.API.Infrastructure.Persistence;
using MassTransit;
using Shared.Domain.IntegrationEvents;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly CarDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public BookingCreatedConsumer(CarDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<BookingCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        if (!message.IncludeCar)
        {
            return;
        }

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
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await _publishEndpoint.Publish(
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
        }
        catch (Exception ex)
        {
            await _publishEndpoint.Publish(
                new BookingFailedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Car,
                    Reason = ex.Message,
                },
                context.CancellationToken
            );
        }
    }
}
