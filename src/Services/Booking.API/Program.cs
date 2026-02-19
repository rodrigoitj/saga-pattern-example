using Booking.API.Application.Mappings;
using Booking.API.Application.Validators;
using Booking.API.Infrastructure.Persistence;
using Booking.API.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Saga.Orchestrator.Application.Interfaces;
using Saga.Orchestrator.Application.Services;
using Saga.Orchestrator.Infrastructure.HttpClients;
using Shared.Domain.Abstractions;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=BookingDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<BookingDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<BaseApplicationDbContext, BookingDbContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookingCommandValidator>();

// builder.Services.AddFluentValidationAutoValidation();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(BookingMappingProfile));

// Add Repositories
builder.Services.AddScoped<IRepository<Booking.API.Domain.Entities.Booking>, BookingRepository>();

// Add Saga Orchestrator services
builder.Services.AddHttpClient<IBookingServiceClient, BookingServiceHttpClient>();
builder.Services.AddScoped<BookingSagaOrchestrator>();

// Add logging
builder.Services.AddLogging();

// Add RabbitMQ (MassTransit)
builder.Services.AddRabbitMqMessaging(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
