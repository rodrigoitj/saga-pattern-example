# Architecture Overview

## System Context

The system uses a message-driven saga to coordinate bookings across independent services. Only the Booking service exposes an HTTP API. All other services listen to RabbitMQ events and respond asynchronously.

```
Client
  │ HTTP
  ▼
Booking.API ── publishes ──► RabbitMQ ◄── consumes ── Flight.API
        │                               ◄── consumes ── Hotel.API
        │                               ◄── consumes ── Car.API
        ▼
PostgreSQL (BookingDb)         PostgreSQL (FlightDb/HotelDb/CarDb)
```

## Event Flow

### Success Path

1. Client sends a booking request to Booking.API.
2. Booking.API stores the booking in `Processing` and publishes `BookingCreatedIntegrationEvent`.
3. Each consumer creates a local booking and publishes `BookingStepCompletedIntegrationEvent`.
4. Booking.API updates the aggregate, and confirms the booking when all required steps complete.

### Failure & Compensation Path

1. A consumer throws or fails to persist a booking.
2. The consumer publishes `BookingFailedIntegrationEvent`.
3. All services consume the failure event and cancel any local bookings tied to that `BookingId`.
4. Booking.API marks the booking as failed and records the failure reason.

## Responsibilities

### Booking.API
- Receives HTTP requests
- Persists booking state and steps
- Publishes `BookingCreatedIntegrationEvent`
- Consumes completion and failure events

### Flight.API / Hotel.API / Car.API
- Consume booking events
- Manage local bookings and compensation
- Publish completion or failure events

## Communication

- **RabbitMQ** for asynchronous messages
- **PostgreSQL** database per service

## Observability and Resilience

- All consumers use MassTransit endpoints
- Compensation is idempotent (safe to repeat)