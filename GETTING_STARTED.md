# Getting Started Guide

## Prerequisites

- **.NET 8.0 SDK** - Download from https://dotnet.microsoft.com/
- **PostgreSQL 14+** - Download from https://www.postgresql.org/
- **Git** - For version control
- **Docker & Docker Compose** (optional) - For containerized deployment

## Local Development Setup

### Step 1: Database Setup

Create four PostgreSQL databases:

```sql
-- Connect to PostgreSQL as admin
psql -U postgres

-- Create databases
CREATE DATABASE BookingDb;
CREATE DATABASE FlightDb;
CREATE DATABASE HotelDb;
CREATE DATABASE CarDb;

-- Verify creation
\l
```

Or using pgAdmin GUI:
1. Right-click "Databases"
2. Select "Create" → "Database"
3. Enter database name and create (repeat for all 4)

### Step 2: Clone and Build

```bash
# Clone the repository
git clone <repository-url>
cd saga-pattern-example

# Restore all projects
dotnet restore

# Build solution
dotnet build
```

### Step 3: Apply Entity Framework Migrations

For each service, apply migrations:

```bash
# Booking Service
cd src/Services/Booking.API
dotnet ef database update
cd ../../..

# Flight Service
cd src/Services/Flight.API
dotnet ef database update
cd ../../..

# Hotel Service
cd src/Services/Hotel.API
dotnet ef database update
cd ../../..

# Car Service
cd src/Services/Car.API
dotnet ef database update
cd ../../..
```

### Step 4: Run Services

Open 4 separate terminal windows and run each service:

**Terminal 1 - Booking Service:**
```bash
cd src/Services/Booking.API
dotnet run
# Runs on http://localhost:5001
```

**Terminal 2 - Flight Service:**
```bash
cd src/Services/Flight.API
dotnet run
# Runs on http://localhost:5002
```

**Terminal 3 - Hotel Service:**
```bash
cd src/Services/Hotel.API
dotnet run
# Runs on http://localhost:5003
```

**Terminal 4 - Car Service:**
```bash
cd src/Services/Car.API
dotnet run
# Runs on http://localhost:5004
```

## Docker Deployment

### Prerequisites
- Docker installed and running
- Docker Compose installed

### Steps

1. **Build Docker images** (from root directory):
```bash
docker-compose up -d --build
```

2. **Verify services are running:**
```bash
docker-compose ps
```

3. **Access services:**
- Booking API: http://localhost:5001
- Flight API: http://localhost:5002
- Hotel API: http://localhost:5003
- Car API: http://localhost:5004

4. **View logs:**
```bash
docker-compose logs -f booking-api
```

5. **Stop services:**
```bash
docker-compose down
```

## Verifying Installation

### 1. Check Service Health via Swagger

Open browser and navigate to:
- http://localhost:5001/swagger (Booking)
- http://localhost:5002/swagger (Flight)
- http://localhost:5003/swagger (Hotel)
- http://localhost:5004/swagger (Car)

### 2. Test Flight Booking

```bash
curl -X POST http://localhost:5002/api/flights \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "departureCity": "New York",
    "arrivalCity": "Los Angeles",
    "departureDateUtc": "2026-03-15T10:00:00Z",
    "arrivalDateUtc": "2026-03-15T13:00:00Z",
    "price": 500.00,
    "passengerCount": 1
  }'
```

### 3. Test Hotel Booking

```bash
curl -X POST http://localhost:5003/api/hotels \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "hotelName": "Luxury Hotel",
    "city": "Los Angeles",
    "checkInDate": "2026-03-15T15:00:00Z",
    "checkOutDate": "2026-03-20T11:00:00Z",
    "roomCount": 1,
    "pricePerNight": 250.00
  }'
```

## Troubleshooting

### Connection String Issues

If you get database connection errors:

1. Verify PostgreSQL is running:
```bash
psql -U postgres -d BookingDb -c "SELECT 1"
```

2. Update connection strings in `appsettings.Development.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=YOUR_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASSWORD;Port=5432"
}
```

### Port Already in Use

If ports 5001-5004 are in use, change in `launchSettings.json`:
```json
"applicationUrl": "http://localhost:XXXX"
```

### Migration Issues

Clear and reapply migrations:
```bash
# In each service directory
dotnet ef migrations add Initial
dotnet ef database update
```

### Services Can't Communicate

Check service URLs in `appsettings.json`:
- Local: Use `http://localhost:5002`, etc.
- Docker: Use `http://service-name:8080`

## Visual Studio Setup

### Opening in Visual Studio

1. Open the solution file: `SagaPattern.sln`
2. Set Booking.API as startup project
3. Configure multiple startup projects:
   - Right-click solution → Properties
   - Select "Multiple startup projects"
   - Set Action to "Start" for all four API projects

### Debugging

1. Set breakpoints in code
2. Press F5 to start debugging
3. Requests will pause at breakpoints

## VS Code Setup

### Extensions
- C# Dev Kit
- REST Client (to test APIs)
- Docker

### Debug Configuration

Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Booking API",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/Services/Booking.API/bin/Debug/net8.0/Booking.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Services/Booking.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "{url}",
        "action": "openExternally"
      }
    }
  ]
}
```

## Next Steps

1. **Explore the code** - Review the clean architecture layers
2. **Test the APIs** - Use Swagger UI or Postman
3. **Create custom bookings** - Extend the domain models
4. **Add authentication** - Implement JWT/OAuth2
5. **Add caching** - Integrate Redis
6. **Add messaging** - Implement with RabbitMQ/Kafka
7. **Add monitoring** - Integrate Prometheus/Grafana

## Common Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run specific service
cd src/Services/Booking.API && dotnet run

# Create new migration
cd src/Services/Booking.API && dotnet ef migrations add MigrationName

# Update database
cd src/Services/Booking.API && dotnet ef database update

# Remove last migration
cd src/Services/Booking.API && dotnet ef migrations remove

# View all migrations
cd src/Services/Booking.API && dotnet ef migrations list
```

## Support

For issues or questions:
1. Check the ARCHITECTURE.md file
2. Review README.md for API examples
3. Check service logs for error messages
4. Verify all services are running on correct ports
