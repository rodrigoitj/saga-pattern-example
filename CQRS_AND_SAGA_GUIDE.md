# CQRS & Saga Pattern Implementation Guide

## CQRS Pattern - Command Query Responsibility Segregation

### What is CQRS?

CQRS separates the responsibility for handling commands (write operations) from queries (read operations). This provides:

- ✅ **Clear Intent** - Commands explicitly state what action is being performed
- ✅ **Scalability** - Read and write models can scale independently
- ✅ **Testability** - Business logic is isolated in handlers
- ✅ **Flexibility** - Different models optimized for different purposes

### Implementation in This Solution

We use **MediatR** library for CQRS implementation:

#### Commands (Write Operations)

```csharp
// Command definition
public class CreateFlightBookingCommand : IRequest<FlightBookingResponseDto>
{
    public Guid UserId { get; set; }
    public string DepartureCity { get; set; }
    // ... properties
}

// Handler
public class CreateFlightBookingCommandHandler 
    : IRequestHandler<CreateFlightBookingCommand, FlightBookingResponseDto>
{
    public async Task<FlightBookingResponseDto> Handle(
        CreateFlightBookingCommand request, 
        CancellationToken cancellationToken)
    {
        // Business logic
    }
}

// Usage in Controller
var result = await _mediator.Send(command, cancellationToken);
```

#### Queries (Read Operations)

```csharp
// Query definition
public class GetFlightBookingByIdQuery : IRequest<FlightBookingResponseDto>
{
    public Guid FlightBookingId { get; set; }
}

// Handler
public class GetFlightBookingByIdQueryHandler 
    : IRequestHandler<GetFlightBookingByIdQuery, FlightBookingResponseDto>
{
    public async Task<FlightBookingResponseDto> Handle(
        GetFlightBookingByIdQuery request, 
        CancellationToken cancellationToken)
    {
        // Read logic (optimized for queries, no side effects)
    }
}

// Usage in Controller
var result = await _mediator.Send(query, cancellationToken);
```

### CQRS Validation with FluentValidation

```csharp
public class CreateFlightBookingCommandValidator 
    : AbstractValidator<CreateFlightBookingCommand>
{
    public CreateFlightBookingCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("UserId is required");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0");

        RuleFor(x => x.DepartureDateUtc)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("DepartureDateUtc must be in the future");
    }
}
```

### Project Structure for CQRS

```
Service.API/
├── Application/
│   ├── Commands/           # Write operations
│   │   └── AddXxxCommand.cs
│   ├── Queries/            # Read operations
│   │   └── GetXxxQuery.cs
│   ├── Handlers/           # Command & Query handlers
│   │   └── XxxHandler.cs
│   ├── Validators/         # FluentValidation rules
│   │   └── XxxValidator.cs
│   ├── DTOs/              # Data transfer objects
│   │   └── XxxDto.cs
│   └── Mappings/          # AutoMapper profiles
│       └── XxxMappingProfile.cs
├── Domain/                # Business logic
├── Infrastructure/        # Data access
└── Presentation/          # Controllers
```

## Saga Pattern - Distributed Transaction Management

### What is the Saga Pattern?

The Saga Pattern is a distributed transaction pattern where a long-running transaction is broken into local transactions that are executed by participating services. 

**Key Concepts:**
- Maintains data consistency across microservices
- Uses compensating transactions for failures
- Services remain loosely coupled
- No distributed locks or two-phase commit

### Two Types of Saga Implementation

#### 1. Choreography (Event-Driven)

Services emit events that trigger other services:
```
Service A → Event → Service B → Event → Service C
```

**Pros:** Simple, loose coupling
**Cons:** Complex to track, debugging difficult

#### 2. Orchestration (Centralized)

A central orchestrator directs each service:
```
Service A ← │ ← Service B
Service C ← │
```

**Pros:** Clear flow, easy to understand
**Cons:** Central point of failure

### Implementation in This Solution

We use **Orchestration** pattern:

```csharp
public class BookingSagaOrchestrator
{
    public async Task<BookingSagaState> ExecuteSagaAsync(
        BookingSagaState sagaState,
        FlightBookingRequest? flightRequest,
        HotelBookingRequest? hotelRequest,
        CarBookingRequest? carRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Book Flight
            if (sagaState.IncludeFlights && flightRequest != null)
            {
                sagaState = await BookFlightAsync(sagaState, flightRequest, cancellationToken);
                if (sagaState.FlightBookingStatus == StepStatus.Failed)
                    throw new InvalidOperationException("Failed to book flight");
            }

            // Step 2: Book Hotel
            if (sagaState.IncludeHotel && hotelRequest != null)
            {
                sagaState = await BookHotelAsync(sagaState, hotelRequest, cancellationToken);
                if (sagaState.HotelBookingStatus == StepStatus.Failed)
                    throw new InvalidOperationException("Failed to book hotel");
            }

            // Step 3: Book Car
            if (sagaState.IncludeCar && carRequest != null)
            {
                sagaState = await BookCarAsync(sagaState, carRequest, cancellationToken);
                if (sagaState.CarBookingStatus == StepStatus.Failed)
                    throw new InvalidOperationException("Failed to book car");
            }

            sagaState.Status = SagaStatus.Completed;
            return sagaState;
        }
        catch (Exception ex)
        {
            // Compensation on failure
            sagaState = await CompensateSagaAsync(sagaState, cancellationToken);
            return sagaState;
        }
    }

    private async Task<BookingSagaState> CompensateSagaAsync(
        BookingSagaState sagaState,
        CancellationToken cancellationToken)
    {
        // Cancel in reverse order (LIFO)
        if (sagaState.CarBookingStatus == StepStatus.Completed)
            await _bookingClient.CancelCarAsync(...);

        if (sagaState.HotelBookingStatus == StepStatus.Completed)
            await _bookingClient.CancelHotelAsync(...);

        if (sagaState.FlightBookingStatus == StepStatus.Completed)
            await _bookingClient.CancelFlightAsync(...);

        sagaState.Status = SagaStatus.Failed;
        return sagaState;
    }
}
```

### Saga State Management

The state machine tracks the saga's progress:

```csharp
public class BookingSagaState
{
    public Guid BookingId { get; set; }
    public SagaStatus Status { get; set; }  // Started, BookingFlights, BookingHotel, etc.
    
    // Step tracking
    public StepStatus FlightBookingStatus { get; set; }
    public StepStatus HotelBookingStatus { get; set; }
    public StepStatus CarBookingStatus { get; set; }
    
    // Compensation tracking
    public List<string> CompensatedSteps { get; set; }
    public string? FailureReason { get; set; }
}
```

### Saga Execution Flow

```
┌─────────────────────┐
│  Booking Requested  │
└──────────┬──────────┘
           │
           ▼
    ┌─────────────────┐
    │ Book Flights    │◄──────┐
    └──────┬┬─────────┘       │
           ││                 │
        OK ││ Failed          │
           ││              Compensation
           ▼▼                 │
    ┌─────────────────┐       │
    │  Book Hotel     │       │
    └──────┬┬─────────┘       │
           ││                 │
        OK ││ Failed          │
           ││                 │
           ▼▼                 │
    ┌─────────────────┐       │
    │   Book Car      │       │
    └──────┬┬─────────┘       │
           ││                 │
        OK ││ Failed          │
           ││                 │
           ▼▼                 │
    ┌─────────────────────┐   │
    │  All Committed      │───┘
    │  OR                 │
    │  All Compensated    │
    └─────────────────────┘
```

## Integration - Combining CQRS and Saga Pattern

The solution combines both patterns:

1. **CQRS handles** individual service operations (Book Flight, Book Hotel, etc.)
2. **Saga Orchestrator handles** the coordination across services

Example flow:

```
1. Client sends CreateBookingCommand
   ↓
2. MediatR routes to CommandHandler
   ↓
3. Handler invokes BookingSagaOrchestrator
   ↓
4. Orchestrator sends commands to each service
   ↓
5. Each service handles via their CQRS Commands/Queries
   ↓
6. Success: All services confirmed
   OR
   Failure: Compensation commands sent to each service
```

## Best Practices

### 1. Keep Commands Simple
```csharp
// Good - Clear intent
public class BookFlightCommand : IRequest<FlightBookingResponseDto>
{
    public Guid UserId { get; set; }
    public string DepartureCity { get; set; }
    // ... only what's needed
}

// Bad - Too much data
public class BookFlightCommand : IRequest<FlightBookingResponseDto>
{
    public FlightBooking Entity { get; set; }  // Don't pass entities
    public string UserProfileData { get; set; } // irrelevant data
}
```

### 2. Validate Commands Early
```csharp
// Validation happens before handler execution
builder.Services.AddFluentValidationAutoValidation();
```

### 3. Keep Handlers Focused
```csharp
// Good - Single responsibility
public class BookFlightCommandHandler : IRequestHandler<BookFlightCommand, Result>
{
    public async Task<Result> Handle(BookFlightCommand request, CancellationToken ct)
    {
        var flight = FlightBooking.Create(...);
        await _repository.AddAsync(flight, ct);
        await _repository.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// Bad - Too many responsibilities
public async Task<Result> Handle(BookFlightCommand request, CancellationToken ct)
{
    // Validation - NO! This is for validator
    // Logging - Let middleware handle this
    // Authorization - Use filters
    // Just business logic
}
```

### 4. Use Compensation Carefully
```csharp
// Compensation must be idempotent - safe to call multiple times
public async Task CancelFlightAsync(Guid flightId)
{
    var flight = await _repository.GetByIdAsync(flightId);
    if (flight.Status == BookingStatus.Confirmed)
    {
        flight.Cancel();  // Safe to repeat
        await _repository.SaveChangesAsync();
    }
}
```

### 5. Saga State Transitions
```csharp
// Valid transitions only
public bool CanTransitionTo(SagaStatus newStatus)
{
    return (Status, newStatus) switch
    {
        (SagaStatus.Started, SagaStatus.BookingFlights) => true,
        (SagaStatus.BookingFlights, SagaStatus.BookingHotel) => true,
        (SagaStatus.BookingHotel, SagaStatus.BookingCar) => true,
        (SagaStatus.BookingCar, SagaStatus.Completed) => true,
        (_, SagaStatus.Compensating) => true,  // Can fail at any point
        (_, SagaStatus.Failed) => true,
        _ => false
    };
}
```

## Extending the Pattern

### Adding New Steps to Saga

```csharp
// 1. Add step to BookingSagaState
HolidayInsuranceBookingStatus { get; set; }

// 2. Add booking step method
private async Task<BookingSagaState> BookHolidayInsuranceAsync(...)

// 3. Add to execution flow
if (sagaState.IncludeInsurance)
    sagaState = await BookHolidayInsuranceAsync(...);

// 4. Add to compensation
if (sagaState.HolidayInsuranceBookingStatus == StepStatus.Completed)
    await _bookingClient.CancelInsuranceAsync(...);
```

### Adding Message Queue Integration

```csharp
// Use Masstransit or NServiceBus for asynchronous saga

public async Task<BookingSagaState> ExecuteSagaAsync(...)
{
    // Send async commands instead of synchronous calls
    await _mediator.Send(new BookFlightCommand(...));
    
    // Saga state machine waits for responses
    // Handles timeouts and retries
}
```

## References

- [Microsoft CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Saga Pattern](https://microservices.io/patterns/data/saga.html)
- [Chris Richardson - Microservices Patterns](https://microservices.io/)
