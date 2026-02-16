FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Hotel.API/Hotel.API.csproj", "Services/Hotel.API/"]
COPY ["src/Common/Domain/Shared.Domain.csproj", "Common/Domain/"]
COPY ["src/Common/Shared/Shared.Infrastructure.csproj", "Common/Shared/"]

RUN dotnet restore "Services/Hotel.API/Hotel.API.csproj"

COPY . .

RUN dotnet build "src/Services/Hotel.API/Hotel.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Services/Hotel.API/Hotel.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hotel.API.dll"]
