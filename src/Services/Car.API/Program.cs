using AutoMapper;
using Car.API.Application.Mappings;
using Car.API.Application.Validators;
using Car.API.Domain.Entities;
using Car.API.Infrastructure.Persistence;
using Car.API.Infrastructure.Repositories;
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
    ?? "Host=localhost;Database=CarDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<CarDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<CreateCarRentalCommandValidator>();

// builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAutoMapper(typeof(CarMappingProfile));
builder.Services.AddScoped<IRepository<CarRental>, CarRentalRepository>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<CarDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
