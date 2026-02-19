using FluentValidation;
using Hotel.API.Domain.Entities;
using Hotel.API.Infrastructure.Persistence;
using Hotel.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Abstractions;
using Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=HotelDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<HotelDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// builder.Services.AddFluentValidationAutoValidation();
// builder.Services.AddValidatorsFromAssemblyContaining<CreateHotelBookingCommandValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // Register validators
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddScoped<IRepository<HotelBooking>, HotelBookingRepository>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
