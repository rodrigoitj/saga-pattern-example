# CQRS & Saga Pattern Guide

This solution uses CQRS only in the Booking API, and coordinates the saga with asynchronous events over RabbitMQ.

## CQRS in Booking.API

- Commands create bookings and publish integration events.
- Queries read booking state and step progress.

## Message-Driven Saga

### Events

- `BookingCreatedIntegrationEvent`
- `BookingStepCompletedIntegrationEvent`
- `BookingFailedIntegrationEvent`

### Orchestration Flow

```
Booking.API -> BookingCreatedIntegrationEvent -> RabbitMQ
  -> Flight/Hotel/Car consumers create local booking
  -> BookingStepCompletedIntegrationEvent -> Booking.API updates aggregate
  -> BookingFailedIntegrationEvent -> all services compensate
```

### Compensation

- Any consumer publishes a failure event when it cannot complete its work.
- All consumers react by cancelling local records tied to the same `BookingId`.
- Booking.API marks the booking as failed.

## Idempotency Notes

- Compensation handlers should be safe to call multiple times.
- Consumers ignore missing bookings during compensation.