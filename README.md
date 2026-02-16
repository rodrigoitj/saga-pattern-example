# Saga Pattern Example - Microservices Booking System

A comprehensive .NET Core 8 microservices application implementing the **Saga Pattern** for distributed transaction management across multiple services (Flight, Hotel, Car bookings).

## 🏗️ Architecture

### Microservices
- **Booking Service** - Orchestrates complete bookings
- **Flight Service** - Manages flight bookings
- **Hotel Service** - Manages hotel reservations
- **Car Service** - Manages car rentals

### Design Patterns & Principles
- ✅ **Saga Pattern** - Orchestrates distributed transactions
- ✅ **Clean Architecture** - Layered separation of concerns
- ✅ **CQRS** - Command Query Responsibility Segregation (MediatR)
- ✅ **Repository Pattern** - Data access abstraction
- ✅ **Unit of Work Pattern** - Transaction management
- ✅ **Dependency Injection** - Loose coupling
- ✅ **SOLID Principles** - Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
- ✅ **Domain-Driven Design** - Domain entities and aggregates
- ✅ **FluentValidation** - Input validation

## 📁 Project Structure

```
src/
├── Services/
│   ├── Booking.API/          # Main booking orchestration service
│   ├── Flight.API/           # Flight booking microservice
│   ├── Hotel.API/            # Hotel reservation microservice
│   └── Car.API/              # Car rental microservice
└── Common/
    ├── Domain/               # Shared domain models and abstractions
    ├── Shared/               # Shared infrastructure (EF Core, UoW)
    └── Saga.Orchestrator/    # Saga orchestration logic

```

## 🛠️ Technology Stack

- **.NET 8.0** - Latest .NET runtime
- **PostgreSQL** - Primary database
- **Entity Framework Core 8.0** - ORM
- **MediatR 12.2.0** - CQRS implementation
- **FluentValidation 11.8.1** - Validation library
- **AutoMapper 13.0.1** - Object mapping
- **Swashbuckle 6.4.6** - OpenAPI/Swagger
- **Docker** - Containerization

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- PostgreSQL 14+
- Docker (optional, for containers)

### Local Development Setup

1. **Clone the repository**
   ```bash
   cd saga-pattern-example
   ```

2. **Update connection strings** in `appsettings.Development.json` files:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=BookingDb;Username=postgres;Password=yourpass;Port=5432"
   }
   ```

3. **Create databases**
   ```sql
   CREATE DATABASE BookingDb;
   CREATE DATABASE FlightDb;
   CREATE DATABASE HotelDb;
   CREATE DATABASE CarDb;
   ```

4. **Apply migrations** (for each service):
   ```bash
   cd src/Services/Booking.API
   dotnet ef database update
   cd ../Flight.API
   dotnet ef database update
   cd ../Hotel.API
   dotnet ef database update
   cd ../Car.API
   dotnet ef database update
   ```

5. **Run services** (from each service directory):
   ```bash
   dotnet run
   ```

   Services will run on:
   - Booking API: http://localhost:5001
   - Flight API: http://localhost:5002
   - Hotel API: http://localhost:5003
   - Car API: http://localhost:5004

### Docker Deployment

```bash
docker-compose up -d
```

Requires first building service Docker images or modifying compose file with local builds.

## 📝 API Usage

### Create a Complete Booking

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

### Book Flight

```http
POST http://localhost:5002/api/flights
Content-Type: application/json

{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "departureCity": "New York",
  "arrivalCity": "Los Angeles",
  "departureDateUtc": "2026-03-15T10:00:00Z",
  "arrivalDateUtc": "2026-03-15T13:00:00Z",
  "price": 500.00,
  "passengerCount": 1
}
```

### Book Hotel

```http
POST http://localhost:5003/api/hotels
Content-Type: application/json

{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "hotelName": "Luxury Hotel",
  "city": "Los Angeles",
  "checkInDate": "2026-03-15T15:00:00Z",
  "checkOutDate": "2026-03-20T11:00:00Z",
  "roomCount": 1,
  "pricePerNight": 250.00
}
```

### Rent Car

```http
POST http://localhost:5004/api/cars
Content-Type: application/json

{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "carModel": "BMW 3 Series",
  "company": "Hertz",
  "pickUpDate": "2026-03-15T14:00:00Z",
  "returnDate": "2026-03-20T10:00:00Z",
  "pickUpLocation": "LAX Airport",
  "pricePerDay": 100.00
}
```

## 🔄 Saga Pattern Workflow

The Saga Pattern ensures ACID properties across distributed services:

1. **Saga Initiation** - Booking service receives booking request
2. **Flight Booking** - Attempts to book flight (if requested)
3. **Hotel Booking** - Attempts to book hotel (if requested)
4. **Car Booking** - Attempts to book car (if requested)
5. **Completion or Compensation**:
   - ✅ **Success**: All bookings confirmed
   - ❌ **Failure**: Compensation logic triggers to cancel completed bookings in reverse order

## 📊 Clean Architecture Layers

### Domain Layer (`Shared.Domain`)
- Entities and Aggregate Roots
- Domain Events
- Repository Interfaces
- Business Rules

### Application Layer
- Commands (CQRS write operations)
- Queries (CQRS read operations)
- Handlers
- DTOs
- Validators (FluentValidation)
- Mapping Profiles (AutoMapper)

### Infrastructure Layer
- EF Core DbContext
- Repository Implementation
- Persistence Configuration
- HTTP Clients

### Presentation Layer (API)
- Controllers
- Request/Response handling
- Error handling

## 🧪 Testing Scenarios

### Success Scenario
1. All three bookings (flight, hotel, car) are available
2. Complete booking is confirmed
3. User receives all confirmation codes

### Failure & Compensation Scenario
1. Flight and hotel bookings succeed
2. Car booking fails
3. Automatic compensation triggers:
   - Hotel reservation is cancelled
   - Flight booking is cancelled
4. User receives failure notification

## 📋 Key Features

- **Distributed Transactions** - Saga pattern ensures consistency across services
- **Automatic Compensation** - Failed bookings trigger automatic rollbacks
- **CQRS Pattern** - Separates read and write operations
- **Input Validation** - FluentValidation for all commands
- **Error Handling** - Comprehensive error handling and logging
- **Domain Events** - Event-driven architecture support
- **API Documentation** - Swagger/OpenAPI integrated in all services
- **Clean, Maintainable Code** - SOLID principles and design patterns

## 🔐 Best Practices Implemented

- ✅ Immutable DTOs with init properties
- ✅ Result pattern for error handling
- ✅ Aggregate roots with domain events
- ✅ Validation at command level
- ✅ Logging throughout the application
- ✅ Async/await for all I/O operations
- ✅ CancellationToken support
- ✅ Thread-safe state management

## 📚 Further Reading

- [Saga Pattern](https://microservices.io/patterns/data/saga.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Domain-Driven Design](https://domainlanguage.com/ddd/)

## 📝 License

This project is provided as an educational example.

## 🤝 Contributing

Feel free to fork, modify, and learn from this codebase!
