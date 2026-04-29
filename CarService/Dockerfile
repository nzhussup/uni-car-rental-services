FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["CarService/CarService.csproj", "CarService/"]
COPY ["CarService.Common/CarService.Common.csproj", "CarService.Common/"]
COPY ["CarRentalService.Data/CarRentalService.Data.csproj", "CarRentalService.Data/"]

RUN dotnet restore "CarService/CarService.csproj"

# Copy everything else and build
COPY . .
WORKDIR /src/CarService
RUN dotnet build "CarService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "CarService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarService.dll"]

