# Project Structure

```
saga-pattern-example/
├── SagaPattern.sln
├── docker-compose.yml
├── docker-compose.dev.yml
├── README.md
├── GETTING_STARTED.md
├── ARCHITECTURE.md
├── CQRS_AND_SAGA_GUIDE.md
├── DOCKER_DEV_GUIDE.md
└── src/
    ├── Services/
    │   ├── Booking.API/          # Only HTTP API
    │   │   ├── Application/
    │   │   │   ├── Commands/
    │   │   │   ├── Consumers/
    │   │   │   ├── Queries/
    │   │   │   └── Handlers/
    │   │   ├── Domain/
    │   │   ├── Infrastructure/
    │   │   └── Presentation/
    │   ├── Flight.API/            # RabbitMQ consumer
    │   ├── Hotel.API/             # RabbitMQ consumer
    │   └── Car.API/               # RabbitMQ consumer
    └── Common/
        ├── Domain/                # Shared abstractions + integration events
        └── Shared/                # Shared infrastructure (EF Core, MassTransit)
```

## Highlights

- **Booking.API** publishes booking events and consumes step results.
- **Flight/Hotel/Car** are worker-style services that only listen to RabbitMQ.
- **Integration event contracts** live in `src/Common/Domain/IntegrationEvents`.