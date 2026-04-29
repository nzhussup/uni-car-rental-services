FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["BookingService/BookingService.csproj", "BookingService/"]
COPY ["BookingService.Common/BookingService.Common.csproj", "BookingService.Common/"]
COPY ["CarRentalService.Data/CarRentalService.Data.csproj", "CarRentalService.Data/"]

RUN dotnet restore "BookingService/BookingService.csproj"

# Copy everything else and build
COPY . .
WORKDIR /src/BookingService
RUN dotnet build "BookingService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BookingService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.dll"]

