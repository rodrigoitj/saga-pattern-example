using Flight.API.Application.Consumers;
using Flight.API.Application.Observability;
using Flight.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("flight-api");
builder.Services.AddSingleton<FlightMetrics>();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=FlightDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<FlightDbContext>(options => options.UseNpgsql(connectionString));

// Add outbox/inbox pattern for reliable messaging
builder.Services.AddOutboxInbox<FlightDbContext>();

// Add RabbitMQ (MassTransit)
builder.Services.AddRabbitMqMessaging(
    builder.Configuration,
    endpointNamePrefix: "flight",
    cfg =>
    {
        cfg.AddConsumer<BookingCreatedConsumer>();
        cfg.AddConsumer<BookingFailedConsumer>();
    },
    useInbox: true
);

var app = builder.Build();

app.UseObservability();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
