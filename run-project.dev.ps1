# Remove windows binaries and obj/bin folders to prevent issues with hardcoded paths in Linux containers
# Write-Host "Cleaning up Windows-generated build artifacts..."
# Get-ChildItem -Path . -Recurse -Include "obj", "bin" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Stop all running containers
# First check if parameter is passed to stop containers, if not, ask the user
if ($args[0] -eq "stop") {
    Write-Host "Stopping all running containers..."
    docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
    exit
}
else {
    $response = Read-Host "Do you want to stop all running containers before starting the development environment? (y/n)"
    if ($response -eq "y") {
        Write-Host "Stopping all running containers..."
        docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
    }
}


# Run the project in development mode using Docker Compose
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build