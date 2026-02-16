# Project Structure & File Summary

## Quick Overview

This is a **microservices-based booking system** that demonstrates enterprise-grade .NET architecture patterns:

- ✅ **4 Independent Microservices** (Booking, Flight, Hotel, Car)
- ✅ **Saga Pattern** for distributed transaction orchestration
- ✅ **CQRS Pattern** with MediatR for command/query separation
- ✅ **Clean Architecture** with clear layer separation
- ✅ **FluentValidation** for input validation
- ✅ **PostgreSQL** with Entity Framework Core
- ✅ **Docker** support for containerization
- ✅ **Swagger/OpenAPI** documentation on all APIs

---

## Directory Structure

```
saga-pattern-example/
├── .gitignore                                    # Git ignore file
├── .git config, other config files              # (git files)
├── SagaPattern.sln                              # Solution file - Open this in Visual Studio
├── docker-compose.yml                           # Docker orchestration file
├── README.md                                    # Main project documentation
├── GETTING_STARTED.md                           # Setup and installation guide
├── ARCHITECTURE.md                              # Architecture overview
├── CQRS_AND_SAGA_GUIDE.md                       # Patterns implementation guide
│
└── src/
    ├── Services/
    │   ├── Booking.API/                         # Main booking orchestration service
    │   │   ├── Booking.API.csproj               # Project file
    │   │   ├── Program.cs                       # Startup configuration
    │   │   ├── appsettings.json                 # Production settings
    │   │   ├── appsettings.Development.json     # Development settings
    │   │   ├── Dockerfile                       # Container image
    │   │   ├── Properties/
    │   │   │   └── launchSettings.json          # Launch configuration
    │   │   ├── Domain/
    │   │   │   ├── Entities/
    │   │   │   │   └── Booking.cs               # Booking aggregate root
    │   │   │   └── Events/
    │   │   │       └── BookingEvents.cs         # Domain events
    │   │   ├── Application/
    │   │   │   ├── Commands/
    │   │   │   │   └── CreateBookingCommand.cs  # CQRS command
    │   │   │   ├── Queries/
    │   │   │   │   └── BookingQueries.cs        # CQRS queries
    │   │   │   ├── Handlers/
    │   │   │   │   ├── CreateBookingCommandHandler.cs
    │   │   │   │   └── QueryHandlers.cs
    │   │   │   ├── Validators/
    │   │   │   │   └── CreateBookingCommandValidator.cs  # FluentValidation
    │   │   │   ├── DTOs/
    │   │   │   │   └── BookingDtos.cs           # Data transfer objects
    │   │   │   └── Mappings/
    │   │   │       └── BookingMappingProfile.cs # AutoMapper config
    │   │   ├── Infrastructure/
    │   │   │   ├── Persistence/
    │   │   │   │   └── BookingDbContext.cs      # EF Core DbContext
    │   │   │   └── Repositories/
    │   │   │       └── BookingRepository.cs     # Repository pattern
    │   │   └── Presentation/
    │   │       └── Controllers/
    │   │           └── BookingsController.cs    # API endpoints
    │   │
    │   ├── Flight.API/                          # Flight booking service
    │   │   ├── Flight.API.csproj
    │   │   ├── Program.cs
    │   │   ├── Dockerfile
    │   │   ├── Domain/
    │   │   │   ├── Entities/
    │   │   │   │   └── FlightBooking.cs
    │   │   │   └── Events/
    │   │   │       └── FlightBookedEvent.cs
    │   │   ├── Application/
    │   │   │   ├── Commands/ → FlightCommands.cs
    │   │   │   ├── Queries/ → FlightQueries.cs
    │   │   │   ├── Handlers/ → FlightHandlers.cs
    │   │   │   ├── Validators/ → FlightValidators.cs
    │   │   │   ├── DTOs/ → FlightDtos.cs
    │   │   │   └── Mappings/ → FlightMappingProfile.cs
    │   │   ├── Infrastructure/
    │   │   │   ├── Persistence/ → FlightDbContext.cs
    │   │   │   └── Repositories/ → FlightBookingRepository.cs
    │   │   └── Presentation/ → FlightsController.cs
    │   │
    │   ├── Hotel.API/                           # Hotel reservation service
    │   │   ├── Hotel.API.csproj
    │   │   ├── Program.cs
    │   │   ├── Dockerfile
    │   │   ├── Domain/
    │   │   │   ├── Entities/ → HotelBooking.cs
    │   │   │   └── Events/ → HotelBookedEvent.cs
    │   │   ├── Application/
    │   │   │   ├── Commands/ → HotelCommands.cs
    │   │   │   ├── Queries/ → HotelQueries.cs
    │   │   │   ├── Handlers/ → HotelHandlers.cs
    │   │   │   ├── Validators/ → HotelValidators.cs
    │   │   │   ├── DTOs/ → HotelDtos.cs
    │   │   │   └── Mappings/ → HotelMappingProfile.cs
    │   │   ├── Infrastructure/
    │   │   │   ├── Persistence/ → HotelDbContext.cs
    │   │   │   └── Repositories/ → HotelBookingRepository.cs
    │   │   └── Presentation/ → HotelsController.cs
    │   │
    │   └── Car.API/                             # Car rental service
    │       ├── Car.API.csproj
    │       ├── Program.cs
    │       ├── Dockerfile
    │       ├── Domain/
    │       │   ├── Entities/ → CarRental.cs
    │       │   └── Events/ → CarRentalBookedEvent.cs
    │       ├── Application/
    │       │   ├── Commands/ → CarCommands.cs
    │       │   ├── Queries/ → CarQueries.cs
    │       │   ├── Handlers/ → CarHandlers.cs
    │       │   ├── Validators/ → CarValidators.cs
    │       │   ├── DTOs/ → CarDtos.cs
    │       │   └── Mappings/ → CarMappingProfile.cs
    │       ├── Infrastructure/
    │       │   ├── Persistence/ → CarDbContext.cs
    │       │   └── Repositories/ → CarRentalRepository.cs
    │       └── Presentation/ → CarsController.cs
    │
    └── Common/
        ├── Domain/                              # Shared domain abstractions
        │   ├── Shared.Domain.csproj
        │   └── Abstractions/
        │       ├── Entity.cs                    # Base entity class
        │       ├── AggregateRoot.cs             # Base aggregate root
        │       ├── IDomainEvent.cs              # Domain event interface
        │       ├── DomainEvent.cs               # Domain event base class
        │       ├── Result.cs                    # Result pattern
        │       ├── IRepository.cs               # Repository interface
        │       └── IUnitOfWork.cs               # Unit of work interface
        │
        ├── Shared/                              # Shared infrastructure
        │   ├── Shared.Infrastructure.csproj
        │   └── Persistence/
        │       ├── BaseApplicationDbContext.cs  # Base DbContext
        │       └── UnitOfWork.cs                # UnitOfWork implementation
        │
        └── Saga.Orchestrator/                   # Saga pattern orchestrator
            ├── Saga.Orchestrator.csproj
            ├── Application/
            │   ├── Interfaces/
            │   │   └── IBookingServiceClient.cs # Service client interface
            │   └── Services/
            │       └── BookingSagaOrchestrator.cs # Main orchestrator
            ├── Domain/
            │   └── Models/
            │       ├── BookingModels.cs         # Request/response models
            │       └── BookingSagaState.cs      # Saga state machine
            └── Infrastructure/
                └── HttpClients/
                    └── BookingServiceHttpClient.cs # HTTP client implementation
```

---

## File Descriptions

### Root Level Files

| File | Purpose |
|------|---------|
| `SagaPattern.sln` | Visual Studio solution file - contains all projects |
| `docker-compose.yml` | Docker Compose for containerized deployment |
| `README.md` | Main project documentation with API examples |
| `GETTING_STARTED.md` | Setup guide for local development and Docker |
| `ARCHITECTURE.md` | Architecture overview and design decisions |
| `CQRS_AND_SAGA_GUIDE.md` | Deep dive into CQRS and Saga pattern implementation |
| `.gitignore` | Git ignore file for Visual Studio .NET projects |

### Shared Domain Layer (`src/Common/Domain`)

| File | Purpose |
|------|---------|
| `Entity.cs` | Base class for all domain entities with ID and timestamps |
| `AggregateRoot.cs` | Base class for aggregate roots with domain event tracking |
| `IDomainEvent.cs` | Interface for domain events |
| `DomainEvent.cs` | Abstract base class for domain events |
| `Result.cs` | Result pattern implementation for better error handling |
| `IRepository.cs` | Generic repository interface defining data operations |
| `IUnitOfWork.cs` | Unit of Work pattern interface for transaction management |

### Shared Infrastructure (`src/Common/Shared`)

| File | Purpose |
|------|---------|
| `BaseApplicationDbContext.cs` | Base DbContext with automatic timestamp management |
| `UnitOfWork.cs` | IUnitOfWork and generic Repository<T> implementation |

### Saga Orchestrator (`src/Common/Saga.Orchestrator`)

| File | Purpose |
|------|---------|
| `IBookingServiceClient.cs` | Interface for communicating with booking services |
| `BookingModels.cs` | Request/response DTOs for saga coordination |
| `BookingSagaState.cs` | Saga state machine tracking booking progress |
| `BookingSagaOrchestrator.cs` | **Core orchestrator** - executes saga with compensation |
| `BookingServiceHttpClient.cs` | HTTP client implementation for service calls |

### Booking Service Pattern (repeated for Flight, Hotel, Car - shown for Booking)

| File | Purpose |
|------|---------|
| `Booking.csproj` | Project configuration with NuGet dependencies |
| `Program.cs` | Service startup and dependency injection configuration |
| `appsettings.json` | Production configuration and connection strings |
| `appsettings.Development.json` | Development-specific configuration |
| `launchSettings.json` | IDE launch profiles |
| `Dockerfile` | Container image definition |
| `Booking.cs` | **Domain:** Aggregate root entity |
| `BookingEvents.cs` | **Domain:** Domain events (BookingCreatedEvent, etc.) |
| `BookingDtos.cs` | **Application:** Data transfer objects |
| `CreateBookingCommand.cs` | **Application:** CQRS write command |
| `BookingQueries.cs` | **Application:** CQRS read queries |
| `CreateBookingCommandValidator.cs` | **Application:** FluentValidation rules |
| `CreateBookingCommandHandler.cs` | **Application:** Command handler implementation |
| `QueryHandlers.cs` | **Application:** Query handler implementations |
| `BookingMappingProfile.cs` | **Application:** AutoMapper configuration |
| `BookingDbContext.cs` | **Infrastructure:** EF Core DbContext |
| `BookingRepository.cs` | **Infrastructure:** Repository implementation |
| `BookingsController.cs` | **Presentation:** REST API controller |

---

## Key Architecture Patterns

### 1. **Clean Architecture** 
- Domain layer (entities, aggregates, events)
- Application layer (commands, queries, validators, handlers)
- Infrastructure layer (EF Core, repositories, HTTP clients)
- Presentation layer (API controllers)

### 2. **CQRS (Command Query Responsibility Segregation)**
- Commands handle write operations (CreateBookingCommand)
- Queries handle read operations (GetBookingByIdQuery)
- Separate handlers for each operation
- Validation at command level (FluentValidation)

### 3. **Saga Pattern for Distributed Transactions**
- Orchestrator coordinates booking across 4 services
- Automatic compensation on failure
- State machine tracking progress
- Idempotent operations for safety

### 4. **Repository Pattern**
- IRepository<T> interface for data access
- Concrete implementations per service
- Dependency injection for testability

### 5. **Domain-Driven Design**
- Aggregate roots (Booking, FlightBooking, etc.)
- Domain events for state changes
- Value objects for better modeling
- Ubiquitous language in code

### 6. **Dependency Injection**
- Constructor injection throughout
- Service registration in Program.cs
- Loose coupling between layers

---

## Technology Stack

| Component | Version | Purpose |
|-----------|---------|---------|
| .NET | 8.0 | Runtime framework |
| PostgreSQL | 14+ | Database |
| Entity Framework Core | 8.0 | ORM |
| MediatR | 12.2.0 | CQRS implementation |
| FluentValidation | 11.8.1 | Input validation |
| AutoMapper | 13.0.1 | Object mapping |
| Swashbuckle | 6.4.6 | OpenAPI/Swagger |
| Docker | Latest | Containerization |

---

## Quick Reference

### Starting Services (Local Development)
```bash
# Booking Service - Port 5001
cd src/Services/Booking.API && dotnet run

# Flight Service - Port 5002
cd src/Services/Flight.API && dotnet run

# Hotel Service - Port 5003
cd src/Services/Hotel.API && dotnet run

# Car Service - Port 5004
cd src/Services/Car.API && dotnet run
```

### Docker Deployment
```bash
docker-compose up -d
```

### Database Setup
```bash
dotnet ef database update  # In each service directory
```

### API Documentation
- Booking: http://localhost:5001/swagger
- Flight: http://localhost:5002/swagger
- Hotel: http://localhost:5003/swagger
- Car: http://localhost:5004/swagger

---

## Learning Path

1. **Start here:** README.md (API overview)
2. **Setup guide:** GETTING_STARTED.md (Local development)
3. **Architecture:** ARCHITECTURE.md (System design)
4. **Patterns:** CQRS_AND_SAGA_GUIDE.md (Deep dive)
5. **Code exploration:** Review services in order:
   - Flight.API (simple service)
   - Hotel.API (domain event usage)
   - Car.API (validation patterns)
   - Booking.API (saga orchestration)
   - Saga.Orchestrator (compensation logic)

---

## Important Notes

- All services use **PostgreSQL** - ensure it's running before testing
- **Services communicate via HTTP** - ensure all are running for complete flows
- **Migrations run automatically** on startup (see Program.cs)
- **Swagger/OpenAPI** available on all services for testing
- Each service has its **own database** (database per service pattern)
- **Validation happens at API layer** via FluentValidation

---

## Next Steps

1. ✅ Clone/review the code
2. ✅ Run local setup (GETTING_STARTED.md)
3. ✅ Explore Swagger documentation
4. ✅ Test APIs manually
5. ✅ Review clean architecture implementation
6. ✅ Understand saga pattern flow
7. ⬜ Extend with authentication (JWT/OAuth)
8. ⬜ Add resilience (Polly)
9. ⬜ Integrate message queue
10. ⬜ Add distributed tracing
