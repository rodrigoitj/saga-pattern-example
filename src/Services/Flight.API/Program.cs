using Flight.API.Application.Mappings;
using Flight.API.Application.Validators;
using Flight.API.Domain.Entities;
using Flight.API.Infrastructure.Persistence;
using Flight.API.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Abstractions;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=FlightDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<FlightDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<CreateFlightBookingCommandValidator>();

// builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAutoMapper(typeof(FlightMappingProfile));
builder.Services.AddScoped<IRepository<FlightBooking>, FlightBookingRepository>();

// Add RabbitMQ (MassTransit)
builder.Services.AddRabbitMqMessaging(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
