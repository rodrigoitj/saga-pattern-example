# Getting Started

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL
- RabbitMQ

## Database Setup

Create databases:

```sql
CREATE DATABASE BookingDb;
CREATE DATABASE FlightDb;
CREATE DATABASE HotelDb;
CREATE DATABASE CarDb;
```

## Build and Run

```bash
dotnet restore
dotnet build

cd src/Services/Booking.API; dotnet run
cd src/Services/Flight.API; dotnet run
cd src/Services/Hotel.API; dotnet run
cd src/Services/Car.API; dotnet run
```

Booking API is available at http://localhost:5001.

## Docker

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```