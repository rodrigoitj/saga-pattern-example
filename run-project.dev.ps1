# Stop all running containers
docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
# Run the project in development mode using Docker Compose
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build