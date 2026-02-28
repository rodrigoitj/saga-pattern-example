# Saga Pattern Example - Message-Driven Booking

A .NET 8 microservices sample that uses RabbitMQ to coordinate bookings across Flight, Hotel, and Car services. The Booking service exposes the only HTTP API and publishes a `BookingCreatedIntegrationEvent` to RabbitMQ. Each consumer creates its local booking record and, on failure, publishes a `BookingFailedIntegrationEvent` to trigger compensation in the other services.

## Architecture

- **Booking.API**: HTTP API + orchestration via events
- **Flight.API**: RabbitMQ consumer (no HTTP endpoints)
- **Hotel.API**: RabbitMQ consumer (no HTTP endpoints)
- **Car.API**: RabbitMQ consumer (no HTTP endpoints)
- **PostgreSQL**: Database per service
- **RabbitMQ**: Event bus for orchestration and compensation

## Core Flow

1. Booking API receives a booking request.
2. Booking API publishes `BookingCreatedIntegrationEvent`.
3. Flight/Hotel/Car consumers create local bookings and publish `BookingStepCompletedIntegrationEvent`.
4. Booking API updates the booking and confirms it once all required steps complete.
5. If a consumer fails, it publishes `BookingFailedIntegrationEvent` and all services compensate.

## Quick Start (Docker)

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

Booking API is available at http://localhost:5001.

## Local Development

1. Start PostgreSQL and RabbitMQ.
2. Restore and build:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Run services (separate terminals):
   ```bash
   cd src/Services/Booking.API; dotnet run
   cd src/Services/Flight.API; dotnet run
   cd src/Services/Hotel.API; dotnet run
   cd src/Services/Car.API; dotnet run
   ```

## API Usage

### Create Booking

```http
POST http://localhost:5001/api/bookings
Content-Type: application/json

{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "checkInDate": "2026-03-15T00:00:00Z",
  "checkOutDate": "2026-03-20T00:00:00Z",
  "includeFlights": true,
  "includeHotel": true,
  "includeCar": true
}
```

### Get Booking Details

```http
GET http://localhost:5001/api/bookings/{bookingId}
```
## Todo list

[x] Add inbox, outbox pattern for reliable message processing
[x] Add observability with OpenTelemetry metrics and logs
[] Add Grafana dashboards for monitoring service health and RabbitMQ queues
[] Add more comprehensive logging with Serilog
[] Add Polly for resilience in HTTP calls and RabbitMQ interactions
[] Write integration tests for the entire saga flow
[] Implement retry policies for transient failures in consumers
[] Add health checks for each service and RabbitMQ connectivity
[] Create a frontend client to interact with the Booking API
[] Configure CI/CD pipelines for automated testing and deployment

## Documentation

- [GETTING_STARTED.md](GETTING_STARTED.md)
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [CQRS_AND_SAGA_GUIDE.md](CQRS_AND_SAGA_GUIDE.md)
- [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md)
- [DOCKER_DEV_GUIDE.md](DOCKER_DEV_GUIDE.md)
- [OBSERVABILITY.md](OBSERVABILITY.md)

## License

Educational use only.