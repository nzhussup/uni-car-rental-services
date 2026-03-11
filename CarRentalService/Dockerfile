FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release


WORKDIR /src
COPY ["CarRentalService.Data/CarRentalService.Data.csproj", "CarRentalService.Data/"]
COPY ["CarRentalService.Web/CarRentalService.Web.csproj", "CarRentalService.Web/"]
RUN dotnet restore "CarRentalService.Web/CarRentalService.Web.csproj"
COPY . .
WORKDIR "/src/CarRentalService.Web"
RUN dotnet build "./CarRentalService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CarRentalService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CarRentalService.Web.dll"]
