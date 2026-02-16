# Architecture Overview

## System Context

The Saga Pattern Example is a distributed booking system that demonstrates how to orchestrate bookings across multiple independent microservices:

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTP
       ▼
┌─────────────────────────┐
│   Booking Service       │ (Orchestrator)
│   - CQRS Pattern        │
│   - Saga Orchestration  │
└──────┬──────────────────┘
       │
   ┌───┴────┬──────────┬───────────┐
   │        │          │           │
   ▼        ▼          ▼           ▼
┌──────┐┌──────┐┌──────┐┌──────┐
│Flight││Hotel ││Car   ││...   │
│API   ││API   ││API   ││APIs  │
└──┬───┘└──┬───┘└──┬───┘└──┬───┘
   │       │       │       │
   └───┬───┴───┬───┴───┬───┘
       │       │       │
       ▼       ▼       ▼
      ┌───────────────────┐
      │  PostgreSQL DB    │
      └───────────────────┘
```

## Data Flow

### Successful Booking Flow

1. **User initiates booking request** to Booking Service
2. **Booking Service creates booking aggregate** with pending status
3. **Saga Orchestrator starts execution**:
   - Calls Flight Service to book flight
   - Receives confirmation code and booking ID
   - Calls Hotel Service to reserve hotel
   - Receives confirmation code and booking ID
   - Calls Car Service to rent car
   - Receives reservation code and booking ID
4. **All bookings confirmed** - Booking marked as confirmed
5. **Response returned** with all confirmation details

### Failed Booking Flow (Compensation)

1. **Saga execution fails** at any step (e.g., Car Service unavailable)
2. **Compensation logic triggers** in reverse order:
   - Cancel car booking (skipped if not yet booked)
   - Cancel hotel reservation
   - Cancel flight booking
3. **All completed bookings are rolled back**
4. **Booking marked as failed** with reason
5. **Error response returned** to client

## Service Responsibilities

### Booking Service (Orchestrator)
- Receives booking requests
- Manages booking state
- Orchestrates saga execution
- Handles compensation

### Flight Service
- Manages flight bookings
- Validates availability
- Performs confirmations and cancellations
- Returns confirmation codes

### Hotel Service
- Manages hotel reservations
- Calculates total price based on nights
- Handles confirmations and cancellations
- Returns confirmation codes

### Car Service
- Manages car rentals
- Calculates total price based on days
- Handles confirmations and cancellations
- Returns reservation codes

## Technology Decisions

### Why Saga Pattern?
- Distributed transaction management without distributed locks
- Compensation logic for failure scenarios
- Service independence and autonomy
- Scalability across multiple services

### Why CQRS?
- Separation of command (write) and query (read) concerns
- Optimized read models
- Better testability
- Clear intent in code (MediatR handlers)

### Why FluentValidation?
- Fluent and readable validation rules
- Reusable validation across layers
- Strong-typed validation
- Better error messages

### Why Clean Architecture?
- Testability
- Maintainability
- Flexibility to change frameworks
- Clear separation of concerns
- Reduced coupling

## Deployment Considerations

### Database Strategy
- **Separate databases per service** (Database per service pattern)
- **PostgreSQL for all services**
- **Automatic migrations on startup**

### Communication
- **Synchronous HTTP/REST** between services
- Could be extended with message queues (RabbitMQ, Kafka) for asynchronous saga

### Resilience
- **Retry logic** on HTTP failures (can be enhanced with Polly)
- **Timeouts** on service calls
- **Circuit breakers** (recommended addition)
- **Health checks** (recommended addition)

## Future Enhancements

1. **Message Queue Integration** (RabbitMQ/Kafka)
   - Asynchronous saga execution
   - Event-driven architecture
   - Better failure handling

2. **Resilience Patterns**
   - Polly for retry and circuit breaker
   - Fallback strategies
   - Bulkhead isolation

3. **Observability**
   - Distributed tracing (Jaeger/Zipkin)
   - Correlation IDs for request tracking
   - Health check endpoints
   - Metrics collection (Prometheus)

4. **Caching**
   - Redis for distributed caching
   - Query optimization
   - Reduced database load

5. **API Gateway**
   - Centralized routing
   - Rate limiting
   - Authentication/Authorization
   - Request transformation

6. **Testing**
   - Unit tests for handlers
   - Integration tests for APIs
   - End-to-end saga tests
   - Contract testing between services

## Security Considerations

1. **Input Validation** - FluentValidation prevents invalid data
2. **Authentication** - Consider adding JWT/OAuth2
3. **Authorization** - Role-based access control
4. **HTTPS** - Enforce in production
5. **Rate Limiting** - Prevent abuse
6. **CORS** - Configure appropriately
7. **Sensitive Data** - Encryption at rest and in transit
