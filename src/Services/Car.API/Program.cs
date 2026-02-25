using Car.API.Application.Consumers;
using Car.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=CarDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<CarDbContext>(options => options.UseNpgsql(connectionString));

// Add outbox/inbox pattern for reliable messaging
builder.Services.AddOutboxInbox<CarDbContext>();

// Add RabbitMQ (MassTransit)
builder.Services.AddRabbitMqMessaging(
    builder.Configuration,
    endpointNamePrefix: "car",
    cfg =>
    {
        cfg.AddConsumer<BookingCreatedConsumer>();
        cfg.AddConsumer<BookingFailedConsumer>();
    },
    useInbox: true
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CarDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
