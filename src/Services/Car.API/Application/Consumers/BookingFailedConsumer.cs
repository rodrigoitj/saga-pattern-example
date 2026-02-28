namespace Car.API.Application.Consumers;

using Car.API.Application.Observability;
using Car.API.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.IntegrationEvents;

public class BookingFailedConsumer : IConsumer<BookingFailedIntegrationEvent>
{
    private readonly CarDbContext _dbContext;
    private readonly CarMetrics _carMetrics;

    public BookingFailedConsumer(CarDbContext dbContext, CarMetrics carMetrics)
    {
        _dbContext = dbContext;
        _carMetrics = carMetrics;
    }

    public async Task Consume(ConsumeContext<BookingFailedIntegrationEvent> context)
    {
        var booking = await _dbContext.CarRentals.FirstOrDefaultAsync(
            c => c.BookingId == context.Message.BookingId,
            context.CancellationToken
        );

        if (booking is null)
        {
            return;
        }

        booking.Cancel();
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        _carMetrics.RecordRentalCancelled();
    }
}
