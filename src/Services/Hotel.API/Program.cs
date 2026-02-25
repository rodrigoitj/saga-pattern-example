using Hotel.API.Application.Consumers;
using Hotel.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=HotelDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<HotelDbContext>(options => options.UseNpgsql(connectionString));

// Add RabbitMQ (MassTransit)
builder.Services.AddRabbitMqMessaging(
    builder.Configuration,
    endpointNamePrefix: "hotel",
    cfg =>
    {
        cfg.AddConsumer<BookingCreatedConsumer>();
        cfg.AddConsumer<BookingFailedConsumer>();
    }
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
