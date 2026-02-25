namespace Hotel.API.Application.Consumers;

using Hotel.API.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.IntegrationEvents;

public class BookingFailedConsumer : IConsumer<BookingFailedIntegrationEvent>
{
    private readonly HotelDbContext _dbContext;

    public BookingFailedConsumer(HotelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<BookingFailedIntegrationEvent> context)
    {
        var booking = await _dbContext.HotelBookings.FirstOrDefaultAsync(
            h => h.BookingId == context.Message.BookingId,
            context.CancellationToken
        );

        if (booking is null)
        {
            return;
        }

        booking.Cancel();
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
