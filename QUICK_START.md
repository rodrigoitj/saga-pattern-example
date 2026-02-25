# Quick Start

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL
- RabbitMQ
- Docker (optional)

## Docker (Recommended)

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

Booking API: http://localhost:5001

## Local Run

```bash
dotnet restore
dotnet build

cd src/Services/Booking.API; dotnet run
cd src/Services/Flight.API; dotnet run
cd src/Services/Hotel.API; dotnet run
cd src/Services/Car.API; dotnet run
```

## Create a Booking

```bash
curl -X POST http://localhost:5001/api/bookings \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "checkInDate": "2026-03-15T00:00:00Z",
    "checkOutDate": "2026-03-20T00:00:00Z",
    "includeFlights": true,
    "includeHotel": true,
    "includeCar": true
  }'
```