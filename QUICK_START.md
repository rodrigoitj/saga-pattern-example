# ⚡ Quick Start Checklist

## 5-Minute Setup (Local Development)

### Prerequisites Check

**Option 1: Docker (Recommended)**
- [ ] Docker Desktop installed and running
- [ ] Git installed (for source control)
- [ ] Visual Studio Code (for debugging)

**Option 2: Local Development**
- [ ] .NET 8.0 SDK installed (`dotnet --version`)
- [ ] PostgreSQL installed and running
- [ ] Git installed (for source control)
- [ ] Visual Studio Code or Visual Studio

### Database Setup (2 minutes)

**If using Docker (Recommended):** Database is created automatically - skip this step!

**If running locally without Docker:**
```bash
# Option 1: Using SQL Client
psql -U postgres -c "CREATE DATABASE BookingDb;"
psql -U postgres -c "CREATE DATABASE FlightDb;"
psql -U postgres -c "CREATE DATABASE HotelDb;"
psql -U postgres -c "CREATE DATABASE CarDb;"

# Option 2: Using pgAdmin GUI
# 1. Open pgAdmin
# 2. Right-click Databases
# 3. Create → Database (repeat 4 times)
```

### Run Services with Docker (RECOMMENDED - 1 minute)
```bash
# Navigate to project root
cd d:\source\saga-pattern-example

# Start all services with Docker Compose (Development mode with Swagger)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Or run in detached mode (background)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d

# View logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# Stop services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
```

### Alternative: Run Services Locally (without Docker)
```bash
# Navigate to your projects folder
cd d:\source\saga-pattern-example

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Set environment variable (PowerShell)
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Terminal 1: Booking Service
cd src/Services/Booking.API; dotnet run

# Terminal 2: Flight Service (new terminal)
$env:ASPNETCORE_ENVIRONMENT = "Development"
cd src/Services/Flight.API; dotnet run

# Terminal 3: Hotel Service (new terminal)
$env:ASPNETCORE_ENVIRONMENT = "Development"
cd src/Services/Hotel.API; dotnet run

# Terminal 4: Car Service (new terminal)
$env:ASPNETCORE_ENVIRONMENT = "Development"
cd src/Services/Car.API; dotnet run
```

### Verify Installation (Open in Browser)
- [ ] http://localhost:5001/swagger (Booking API) ✅
- [ ] http://localhost:5002/swagger (Flight API) ✅
- [ ] http://localhost:5003/swagger (Hotel API) ✅
- [ ] http://localhost:5004/swagger (Car API) ✅

**Note:** If you see 404 errors, ensure you're using both compose files: `-f docker-compose.yml -f docker-compose.dev.yml`

---

## 30-Second API Test

### Copy & Paste in Postman or curl:

**Create a Flight Booking:**
```bash
curl -X POST http://localhost:5002/api/flights \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "departureCity": "New York",
    "arrivalCity": "Los Angeles",
    "departureDateUtc": "2026-03-20T10:00:00Z",
    "arrivalDateUtc": "2026-03-20T13:00:00Z",
    "price": 450.00,
    "passengerCount": 1
  }'
```

**Expected Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "confirmationCode": "FL202603201234ABCD",
  "status": "Pending",
  "price": 450.00,
  // ... more details
}
```

---

## Project Structure at a Glance

```
src/Services/
├── Booking.API/     ← Start here (Orchestrator)
├── Flight.API/      ← Flight bookings
├── Hotel.API/       ← Hotel reservations
└── Car.API/         ← Car rentals

src/Common/
├── Domain/          ← Shared abstractions
├── Shared/          ← Shared infrastructure
└── Saga.Orchestrator/ ← Saga coordination
```

---

## 📖 Documentation Quick Links

| Need | Read This |
|---Docker Development** | DOCKER_DEV_GUIDE.md ⭐ |
| **---|-----------|
| **Overview** | README.md |
| **Setup help** | GETTING_STARTED.md |
| **Architecture** | ARCHITECTURE.md |
| **CQRS & Saga** | CQRS_AND_SAGA_GUIDE.md |
| **File reference** | PROJECT_STRUCTURE.md |
| **What's included** | COMPLETION_SUMMARY.md |

---

## 🐳 Docker Development Features

### What's Included
✅ **Hot Reload** - Code changes auto-reload in running containers  
✅ **Swagger UI** - Enabled in Development mode  
✅ **PostgreSQL** - Automatic database setup  
✅ **Volume Mounts** - Edit code without rebuilding  
✅ **Debug Support** - Attach VS Code debugger to containers  

### Common Docker Commands
```bash
# Start services
docker-compose -f docker-compose.dev.yml up -d

# View logs (all services)
docker-compose -f docker-compose.dev.yml logs -f

# View logs (specific service)
docker-compose -f docker-compose.dev.yml logs -f flight-api

# Restart a service
dockSwagger returns 404 error
**Problem:** `Request reached the end of the middleware pipeline`  
**Solution:** 
- **Using Docker:** Use both compose files (dev overrides production):
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```
- **Running locally:** Set environment variable:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

### Docker: Port already in use
```bash
# Find process using the port
netstat -ano | findstr :5001

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F

# Or stop conflicting Docker containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
```

### Docker: Container won't start
```bash
# Check container logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs flight-api

# Rebuild from scratch
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Docker: Database connection issues
```bash
# Wait for PostgreSQL to be ready
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs postgres

# Check if postgres is healthy
docker inspect saga-dev-postgres --format='{{.State.Health.Status}}'

# Should show: healthy
```

### "Connection refused" error (Local Development)
- [ ] PostgreSQL is running
- [ ] Connection string is correct (check appsettings.Development.json)
- [ ] Database names match your setup
# Stop and remove volumes
docker-compose -f docker-compose.dev.yml down -v

# Check running containers
docker-compose -f docker-compose.dev.yml ps

# Execute commands in container
docker exec -it flight-api-dev /bin/bash
```

### Debugging in VS Code
1. Start services: `docker-compose -f docker-compose.dev.yml up -d`
2. Open VS Code Run and Debug (Ctrl+Shift+D)
3. Select "Docker: Attach to Flight API" (or any service)
4. Set breakpoints in your code
5. Make API calls - debugger will hit breakpoints

**Note:** First attach may be slow while debugger tools install in container.

### Production vs Development

| Feature | docker-compose.yml | docker-compose.dev.yml |
|---------|-------------------|------------------------|
| Environment | Production | Development |
| Swagger | ❌ Disabled | ✅ Enabled |
| Hot Reload | ❌ No | ✅ Yes |
| Debugging | ❌ No | ✅ Yes |
| Volume Mounts | ❌ No | ✅ Yes |
| Image Size | Smaller | Larger (includes SDK) |

---

## Troubleshooting

### "Connection refused" error
- [ ] PostgreSQL is running
- [ ] Connection string is correct (check appsettings.Development.json)
- [ ] Database names match your setup

### "Port already in use"
- [ ] Kill process on port 5001-5004
- [ ] Or change ports in launchSettings.json

### API returns 404
- [ ] Ensure all services are running
- [ ] Check service URLs match (localhost vs container names)

### Swagger won't load
- [ ] Give service a few seconds to start
- [ ] Check browser console for errors
- [ ] Verify API is responding: `curl http://localhost:5001/health`

---

## Common Tasks

### View Database Data

**Using Docker:**
```bash
# Connect to PostgreSQL
docker exec -it saga-dev-postgres psql -U postgres -d FlightDb

# Show tables
\dt

# View booking data
SELECT * FROM "FlightBookings";

# Exit
\q
```

**Running Locally:**
```sql
-- Connect to PostgreSQL
psql -U postgres -d BookingDb

-- Show tables
\dt

-- View booking data
SELECT * FROM "Bookings";
```

### Check Service Logs

**Using Docker:**
```bash
# View all logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# View specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f flight-api
```

**Running Locally:**
```bash
# In each service terminal, you'll see detailed logs
# Look for INFO, WARNING, ERROR messages
```

### Restart a Service

**Using Docker:**
```bash
# Restart specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart flight-api

# Rebuild and restart
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d flight-api
```

**Running Locally:**
```bash
# Ctrl+C in the terminal, then run again
cd src/Services/Flight.API
dotnet run
```

### Test Saga Compensation
1. Create full booking (flights + hotel + car)
2. Expect failure in car service (will fail)
3. Watch console as services are automatically compensated
4. Check database - bookings should be cancelled

### Extend the Application
1. Add new field to entity
2. Create migration: `dotnet ef migrations add AddNewField`
3. Update DbContext model
4. Create new Command/Query
5. Add validator
6. Update Database: `dotnet ef database update`

---

## Performance Optimization Tips

- Add Redis caching for frequently queried data
- Implement connection pooling
- Add indexes to frequently searched columns
- Use async/await (already implemented)
- Implement pagination for large lists

---

## Security Checklist

- [ ] Enable HTTPS in production
- [ ] Add authentication (JWT/OAuth)
- [ ] Add authorization (roles/policies)
- [ ] Validate and sanitize all inputs (FluentValidation in place)
- [ ] Add rate limiting
- [ ] Use environment variables for secrets
- [ ] Enable CORS properly
- [ ] Add API key authentication

---

## Monitoring & Logging Setup

### Add Structured Logging
Already integrated with built-in logging, future enhancements:
- Add Serilog for structured logging
- Add Application Insights
- Add Prometheus metrics
- Add distributed tracing (Jaeger)

---

## Version Control

```bash
# Initialize git (if needed)
git init

# Add all files
git add .

# Commit initial version
git commit -m "Initial microservices saga pattern implementation"

# Create remotes and push
# ... (your git workflow)
```

---

## Success Criteria Checklist

- ✅ Can run `dotnet build` - no errors
- ✅ Can run all 4 services - no crashes
- ✅ Can access Swagger on all services
- ✅ Can make API calls via Swagger
- ✅ Can create bookings end-to-end
- ✅ Database tables created automatically
- ✅ Logs show service communication
- ✅ Docker-compose runs all services

---

## Next Level Tasks

1. **Authentication**: Add JWT token validation
2. **Resilience**: Add Polly retry policies
3. **Caching**: Add Redis for query results
4. **Events**: Implement event publishing
5. **Monitoring**: Add health checks
6. **Tracing**: Add distributed tracing
7. **Testing**: Add unit and integration tests
8. **API Gateway**: Add Ocelot or similar
9. **Message Queue**: Add RabbitMQ/Kafka
10. **Logging**: Add Serilog + Elasticsearch

---

## Common Errors & Solutions

| Error | Solution |
|-------|----------|
| `Connection timeout` | Start PostgreSQL |
| `Port 5001 in use` | Close other apps or change port |
| `DbContext not created` | Run `dotnet ef database update` |
| `Services won't talk` | Check URLs in appsettings |
| `Migration failed` | Delete previous migrations and start fresh |

---

## Key Files to Review First

1. **src/Services/Flight.API/Program.cs** - Service setup
2. **src/Services/Flight.API/Domain/Entities/FlightBooking.cs** - Domain model
3. **src/Services/Flight.API/Application/Commands/FlightCommands.cs** - CQRS commands
4. **src/Services/Flight.API/Application/Handlers/FlightHandlers.cs** - Command handlers
5. **src/Common/Saga.Orchestrator/Application/Services/BookingSagaOrchestrator.cs** - Saga logic

---

## Code Navigation

- **Business Logic**: Under `Domain/Entities/` and `Application/Handlers/`
- **API Routes**: Under `Presentation/Controllers/`
- **Database Config**: Under `Infrastructure/Persistence/`
- **Validation**: Under `Application/Validators/`
- **Data Mapping**: Under `Application/Mappings/`

---

## Time Investment vs. Learning

| Task | Time | Benefit |
|------|------|---------|
| Setup | 5 min | Working environment |
| First API call | 5 min | Verify it works |
| Review Flight API | 15 min | Understand pattern |
| Review Booking API | 20 min | See integration |
| Study Saga Orchestrator | 20 min | Master orchestration |
| Full deeper understanding | 1-2 hrs | Expert level |

---

## Questions?

Check these in order:
1. README.md - Overview
2. GETTING_STARTED.md - Installation
3. ARCHITECTURE.md - System design
4. CQRS_AND_SAGA_GUIDE.md - Patterns
5. Console logs - Runtime issues
6. Code comments - Implementation details

---

## Final Tips

✨ **Pro Tips:**
- **Docker Development (Recommended):**
  - Use `docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d` for background operation
  - Hot reload works automatically - just save your files
  - Attach VS Code debugger for step-through debugging
  - Check logs with `docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f`
  
- **General:**
  - Keep all 4 services running for full functionality
  - Use Swagger for easy API testing  
  - Database gets created automatically
  - Each service is independent - can run in isolation

🚀 **You're ready to go!**

**Quick Start with Docker:**
```bash
# One command to rule them all
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Open http://localhost:5001/swagger in browser
```

For detailed Docker usage, see [DOCKER_DEV_GUIDE.md](DOCKER_DEV_GUIDE.md)
