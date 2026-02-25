namespace Hotel.API.Application.Consumers;

using Hotel.API.Domain.Entities;
using Hotel.API.Infrastructure.Persistence;
using MassTransit;
using Shared.Domain.IntegrationEvents;

public class BookingCreatedConsumer : IConsumer<BookingCreatedIntegrationEvent>
{
    private readonly HotelDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public BookingCreatedConsumer(HotelDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
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
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            await _publishEndpoint.Publish(
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
        }
        catch (Exception ex)
        {
            await _publishEndpoint.Publish(
                new BookingFailedIntegrationEvent
                {
                    BookingId = message.BookingId,
                    StepType = BookingStepType.Hotel,
                    Reason = ex.Message,
                },
                context.CancellationToken
            );
        }
    }
}
