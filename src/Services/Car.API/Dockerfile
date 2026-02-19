FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NuGet.Config", "./"]

COPY ["src/Services/Car.API/Car.API.csproj", "Services/Car.API/"]
COPY ["src/Common/Domain/Shared.Domain.csproj", "Common/Domain/"]
COPY ["src/Common/Shared/Shared.Infrastructure.csproj", "Common/Shared/"]

ENV NUGET_FALLBACK_PACKAGES=

RUN dotnet restore "Services/Car.API/Car.API.csproj"

COPY . .

RUN dotnet build "src/Services/Car.API/Car.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Car.API/Car.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Car.API.dll"]
