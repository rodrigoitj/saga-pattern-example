namespace Flight.API.Application.Consumers;

using Flight.API.Application.Observability;
using Flight.API.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.IntegrationEvents;

public class BookingFailedConsumer : IConsumer<BookingFailedIntegrationEvent>
{
    private readonly FlightDbContext _dbContext;
    private readonly FlightMetrics _flightMetrics;

    public BookingFailedConsumer(FlightDbContext dbContext, FlightMetrics flightMetrics)
    {
        _dbContext = dbContext;
        _flightMetrics = flightMetrics;
    }

    public async Task Consume(ConsumeContext<BookingFailedIntegrationEvent> context)
    {
        var booking = await _dbContext.FlightBookings.FirstOrDefaultAsync(
            f => f.BookingId == context.Message.BookingId,
            context.CancellationToken
        );

        if (booking is null)
        {
            return;
        }

        booking.Cancel();
        await _dbContext.SaveChangesAsync(context.CancellationToken);
        _flightMetrics.RecordReservationCancelled();
    }
}
