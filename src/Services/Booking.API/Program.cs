using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Booking.API.Infrastructure.Persistence;
using Booking.API.Infrastructure.Repositories;
using Booking.API.Application.Mappings;
using Booking.API.Application.Validators;
using Shared.Domain.Abstractions;
using Shared.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=BookingDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(connectionString));

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

// Add logging
builder.Services.AddLogging();

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
