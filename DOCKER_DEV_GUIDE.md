# Docker Development Guide

## Start the Stack

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build
```

## Services

- Booking API: http://localhost:5001
- RabbitMQ: http://localhost:15672 (guest/guest)

Flight/Hotel/Car run as background consumers and do not expose HTTP ports.

## Logs

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml logs -f
```