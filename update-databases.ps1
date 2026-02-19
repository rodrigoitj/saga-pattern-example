# Apply Entity Framework Migrations for all services

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore

# Booking Service
Write-Host "Updating Booking Service database..." -ForegroundColor Cyan
cd src/Services/Booking.API
dotnet ef database update
cd ../../..

# Flight Service
Write-Host "Updating Flight Service database..." -ForegroundColor Cyan
cd src/Services/Flight.API
dotnet ef database update
cd ../../..

# Hotel Service
Write-Host "Updating Hotel Service database..." -ForegroundColor Cyan
cd src/Services/Hotel.API
dotnet ef database update
cd ../../..

# Car Service
Write-Host "Updating Car Service database..." -ForegroundColor Cyan
cd src/Services/Car.API
dotnet ef database update
cd ../../..

Write-Host "Database migrations completed!" -ForegroundColor Green
