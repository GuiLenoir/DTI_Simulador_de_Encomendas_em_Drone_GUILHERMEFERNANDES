FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/DroneDelivery.Api/DroneDelivery.Api.csproj backend/DroneDelivery.Api/
RUN dotnet restore backend/DroneDelivery.Api/DroneDelivery.Api.csproj
COPY backend/DroneDelivery.Api/ backend/DroneDelivery.Api/
RUN dotnet publish backend/DroneDelivery.Api/DroneDelivery.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "dotnet DroneDelivery.Api.dll --urls http://+:${PORT:-8080}"]
