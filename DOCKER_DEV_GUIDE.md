# 🐳 Docker Development Guide

## Quick Start

Start all services in development mode with Swagger enabled:

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

Access Swagger UI:
- **Booking API**: http://localhost:5001/swagger
- **Flight API**: http://localhost:5002/swagger
- **Hotel API**: http://localhost:5003/swagger
- **Car API**: http://localhost:5004/swagger

## How It Works

The development setup uses **Docker Compose override** pattern:

1. **docker-compose.yml** - Base configuration (production-ready)
2. **docker-compose.dev.yml** - Development overrides (extends base)

When you specify both files, Docker Compose merges them, with dev settings overriding production settings.

```bash
# Base + Development overrides
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

## Development Setup Features

✅ **ASPNETCORE_ENVIRONMENT=Development** - Swagger and detailed errors enabled  
✅ **Hot Reload** - Code changes automatically reload  
✅ **Volume Mounts** - Edit code without rebuilding containers  
✅ **PostgreSQL** - Automatic database creation and migrations  
✅ **Debugger Support** - Attach VS Code debugger to running containers  

## File Structure

```
.
├── docker-compose.yml          # Base configuration (shared settings)
├── docker-compose.dev.yml      # Development overrides only
└── src/Services/
    ├── Booking.API/
    │   ├── Dockerfile          # Production build
    │   └── Dockerfile.dev      # Development build
    ├── Flight.API/
    │   ├── Dockerfile
    │   └── Dockerfile.dev
    ├── Hotel.API/
    │   ├── Dockerfile
    │   └── Dockerfile.dev
    └── Car.API/
        ├── Dockerfile
        └── Dockerfile.dev
```

## What Gets Overridden?

**docker-compose.dev.yml** only changes:
- ✅ Environment: `Development` (enables Swagger)
- ✅ Image names: `*-api:dev` instead of `*-api:latest`
- ✅ Container names: `*-api-dev` yml -f docker-compose.dev.yml up --build

# Start in background
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d

# Start and watch logs
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### View Logs
```bash
# All services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f

# Specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f flight-api

# Last 100 lines
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs --tail=100 flight-api
```

### Manage Services
```bash
# Restart a service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart flight-api

# Stop all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml stop

# Stop and remove containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down

# Stop and remove volumes (deletes database data)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down -v
```

### Rebuild Services
```bash
# Rebuild specific service
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build flight-api

# Rebuild all services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build

# Rebuild without cache (clean build)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache

# Rebuild and restart
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d
```

### Container Access
```bash
# Execute bash in container
docker exec -it flight-api-dev /bin/bash

# Execute single command
docker exec -it flight-api-dev dotnet --version

# View running containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml ps

# View all containers (including stopped)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml build --no-cache

# Rebuild and restart
docker-compose -f docker-compose.dev.yml up --build -d
```

### Container Access
```bash
# Execute bash in container
docker exec -it flight-api-dev /bin/bash

# Execute single command
docker exec -it flight-api-dev dotnet --version

# View running containers
docker-compose -f docker-compose.dev.yml ps

# View all containers (including stopped)
docker-compose -f docker-compose.dev.yml ps -a
```

### Database Access
```bash
# Connect to PostgreSQL
docker exec -it saga-dev-postgres psql -U postgres

# List databases
docker exec -it saga-dev-postgres psql -U postgres -c "\l"
yml -f docker-compose.
# Query a database
docker exec -it saga-dev-postgres psql -U postgres -d FlightDb -c "SELECT * FROM \"FlightBookings\";"

# Backup database
docker exec saga-dev-postgres pg_dump -U postgres FlightDb > flight_backup.sql

# Restore database
cat flight_backup.sql | docker exec -i saga-dev-postgres psql -U postgres FlightDb
```

## Debugging with VS Code

### Setup
1. Open workspace in VS Code
2. Ensure `.vscode/launch.json` exists (already created)
3. Start services:
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

### Attach Debugger
1. Press `Ctrl+Shift+D` (Run and Debug)
2. Select configuration:
   - "Docker: Attach to Flight API"
   - "Docker: Attach to Booking API"
   - "Docker: Attach to Hotel API"
   - "Docker: Attach to Car API"
3. Press F5 to attach
4. Set breakpoints in your code
5. Make API calls via Swagger or Postman

### Debug Ports
- Booking API: 5011
- Flight API: 5012
- Hotel API: 5013
- Car API: 5014

## Hot Reload

The development containers use `dotnet watch run` which automatically reloads when you save files:

1. Make changes to any `.cs` file
2. Save the file (Ctrl+S)
3. Watch container logs - you'll see:
   ```
   dotnet watch ⌚ File changed: /app/Flight.API/Controllers/FlightsController.cs
   dotnet watch 🔥 Hot reload of changes succeeded.
   ```
4. Changes are immediately available

**Note:** Some changes (project files, dependencies) require rebuild:
```bash
docker-compose -f docker-compose.dev.yml restart flight-api
```

## Production vs Development

| Feature | Production | Development |
|---------|-----------|-------------|
| **File** | docker-compose.yml | docker-compose.yml + docker-compose.dev.yml |
| **Environment** | Production | Development |
| **Base Image** | aspnet:8.0 (runtime only) | sdk:8.0 (full SDK) |
| **Swagger** | ❌ Disabled | ✅ Enabled |
| **Hot Reload** | ❌ No | ✅ Yes (dotnet watch) |
| **Debugging** | ❌ No | ✅ Yes |
| **Volume Mounts** | ❌ No | ✅ Yes (src code) |
| **Build** | Multi-stage optimized | Single stage |
| **Size** | ~200MB | ~700MB |
| **Error Details** | Generic | Detailed stack traces |

### Running in Production
```bash
# Production only (no dev overrides)
docker-compose up -d

# Or explicitly
docker-compose -f docker-compose.yml up -d
```

## Environment Variables

Development containers use these environment variables:

```yaml
ASPNETCORE_ENVIRONMENT=Development          # Enables Swagger, detailed errors
ASPNETCORE_URLS=http://+:8080              # Listen on all interfaces
ConnectionStrings__DefaultConnection=...    # PostgreSQL connection
```

## Volumes

### Code Volumes (Hot Reload)
```yaml
volumes:
  - ./src/Services/Flight.API:/app/Flight.API
  - ./src/Common:/app/Common
```
yml -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.ymlolume (Persistence)
```yaml
volumes:
  - postgres_dev_data:/var/lib/postgresql/data
```

To reset database:
```bash
docker-compose -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.dev.yml up -d
```

## Networking

All containers run on `saga-dev-network`:

```
booking-api → http://flight-api:8080
booking-api → http://hotel-api:8080only  
**Solution:** Use both files (dev overrides)

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

### Port Conflicts
**Problem:** Port 5001-5004 already in use  
**Solution:**
```bash
# Stop other services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down

# Or change ports in docker-compose

### Port Conflicts
**Problem:** Port 5001-5004 already in use  
**Solution:**
```bash
# Stop other services
docker-compose -f docker-compose.dev.yml down
yml -f docker-compose.dev.yml restart flight-api
```

### Hot Reload Not Working
**Problem:** Changes not reflecting  
**Solution:**
1. Check logs for compilation errors
2. Ensure file is saved
3. Some changes require restart:
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart flight-api
   ```

### Container Keeps Restarting
**Problem:** Application crashes on startup  
**Solution:**
```bash
# Check logs
docker-compose -f docker-compose.yml
   docker-compose -f docker-compose.dev.yml restart flight-api
   ```

### Container Keeps Restarting
**Problem:** Application crashes on startup  
**Solution:**
```bash
# Check logs
docker-compose -f docker-compose.dev.yml logs flight-api

# Common causes:
# - Database connection failed
# - Code compilation erroryml -f docker-compose.dev.yml build flight-api
docker-compose -f docker-compose.ymlpendencies
```

## Performance Tips

### Faster Rebuilds
```bash
# Only rebuild changed service
docker-compose -f docker-compose.dev.yml build flight-api
docker-compose -f docker-compose.dev.yml up -d flight-api
```

### Cleanup Unused Resources
```bash
# Remove stopped containers
docker container prune

# Remove unused images
docker image prune -a

# Remove unused volumes
docker volume prune

# Remove everything (CAREFUL!)
docker system prune -a --volumes
```

## Testing in Docker

### Run Unit Tests
```bash
# Execute tests in running container
docker exec -it flight-api-dev dotnet test

# Or create a test-specific container
docker-compose -f docker-compose.test.yml up --abort-on-container-exit
```

### API Testing
```bash
# Health check
curl http://localhost:5002/health

# Create booking
curl -X POST http://localhost:5002/api/flights \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440001",
    "departureCity": "NYC",
    "arrivalCity": "LAX",
    "departureDateUtc": "2026-03-20T10:00:00Z",
    "arrivalDateUtc": "2026-03-20T13:00:00Z",
    "price": 450.00,
    "passengerCount": 1
  }'
```yml -f docker-compose.dev.yml up -d
   ```
3. Same URLs work: http://localhost:5001-5004

### From Docker to Local Development

1. Stop Docker services:
   ```bash
   docker-compose -f docker-compose.yml
   docker-compose -f docker-compose.dev.yml up -d
   ```
3. Same URLs work: http://localhost:5001-5004

### From Docker to Local Development

1. Stop Docker services:
   ```bash
   docker-compose -f docker-compose.dev.yml down
   ```
2. Ensure PostgreSQL is running locally
3. Set environment and run:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   cd src/Services/Flight.API
   dotnet run
   ```

## Next Steps

- 📖 Read [QUICK_START.md](QUICK_START.md) for API testing
- 🏗️ Read [ARCHITECTURE.md](ARCHITECTURE.md) for system design
- 🔄 Read [CQRS_AND_SAGA_GUIDE.md](CQRS_AND_SAGA_GUIDE.md) for patterns
- 🐛 Use VS Code debugger to step through code
- 🧪 Add integration tests with Docker

## Additional Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [ASP.NET Core in Docker](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [Debugging in Docker](https://code.visualstudio.com/docs/containers/debug-common)
