# Booking Service Documentation

## Overview and Responsibility

`BookingService` is the booking-focused REST API in the split backend. It stores bookings in SQL Server, enforces user and admin access rules with Keycloak JWTs, converts money through the gRPC currency service client, and exchanges booking state with `CarService` over RabbitMQ.

## Tools and Technologies Used

- ASP.NET Core Web API on .NET
- Entity Framework Core with SQL Server
- Keycloak AuthServices for JWT authentication and realm-role authorization
- AutoMapper for DTO mapping
- RabbitMQ.Client for asynchronous integration
- Swagger / OpenAPI for contract generation

## API Description

Base route: `/api/booking`

| Method | Route | Purpose | Authorization |
| --- | --- | --- | --- |
| `GET` | `/api/booking` | List all bookings with pagination and optional currency conversion | `app-admin` |
| `GET` | `/api/booking/user` | List bookings for the current authenticated user | `app-user` |
| `GET` | `/api/booking/{id}` | Read one booking | owner or admin |
| `POST` | `/api/booking` | Create a booking for the current user | `app-user` |
| `PATCH` | `/api/booking/{id}/status` | Update booking status | `app-admin` |
| `PATCH` | `/api/booking/{id}/cancel` | Cancel the current user's booking | `app-user` |
| `DELETE` | `/api/booking/{id}` | Delete a booking and release reserved dates | `app-admin` |

Important query parameters:

- `Skip` and `Take` are required for list endpoints.
- `Take` is bounded to `50` by the generated contract.
- `targetCurrency` defaults to `USD`.

## Data Model

The service stores bookings and enriches responses with user profile and converted price data.

```text
Booking
|- Id
|- CarId
|- UserId (Keycloak subject UUID)
|- PickupDate
|- DropoffDate
|- BookingDate
|- Status
|- CarPriceInUsd
|- TotalCostInUsd
|- Make / Model / CarYear snapshot
```

Runtime enrichment:

- `UserService` resolves user profile data from Keycloak.
- `IExtCurrencyConvertService` converts `TotalCost` into the requested currency.
- `CarSubscriber` updates booking snapshots after `CarService` responds to `booking.info` events.

## Runtime Wiring and Dependencies

```text
Frontend -> NginxGateway -> BookingService
BookingService -> SQL Server
BookingService -> Keycloak
BookingService -> CurrencyConverterService (gRPC client)
BookingService -> RabbitMQ exchange: booking.*
BookingService <- RabbitMQ exchange: car.*
```

Startup wiring from `Program.cs`:

- SQL Server `BookingDbContext`
- Keycloak JWT bearer authentication and `User` / `Admin` policies
- gRPC client for `CurrencyConverter.CurrencyConverterClient`
- RabbitMQ producer plus background subscriber
- global exception handling and Problem Details
- development Swagger UI and frontend CORS policy

RabbitMQ integration details:

- published exchange: `booking_exchange`
- published routing key: `booking.info`
- published payload type: `BookingInfo`
- subscribed exchange: `car_exchange`
- subscribed queue: `car_queue`
- subscribed routing pattern: `car.*`

Consumed payload types:

- `CarInfo`
  - sent by `CarService` after processing a booking request
  - contains the booking id, car snapshot fields, availability result, and price data
- `MaintainanceStartInfo`
  - sent by `CarService` when a car is deleted or switched to maintenance
  - used to cancel affected bookings

Event flow:

1. `BookingService` writes the booking and publishes `BookingInfo`
2. `CarService` consumes that message and checks whether the car can be reserved
3. `CarService` publishes `CarInfo` back to the car exchange
4. `BookingService` consumes `CarInfo` and finalizes the booking snapshot and status
5. maintenance events trigger cancellation of affected bookings

## Validation and Error Handling

Validation and business checks come from both ASP.NET model binding and service logic:

- missing authenticated user id returns `401`
- missing booking or inaccessible booking returns `404` or `403`
- overlapping active bookings for the same car are rejected
- completed bookings cannot be changed
- cancel operations reject invalid ownership or duplicate cancellation
- incomplete `CurrencyConverterSettings` or `KeycloakAdmin` configuration fails startup

`GlobalExceptionHandlerMiddleware` maps domain exceptions to JSON Problem Details:

- `NotFoundException` -> `404`
- `NotAllowedException` -> `403`
- `UserIdNotFoundException` -> `401`
- unexpected exceptions -> `500`

## Code Quality Practices

- OpenAPI is generated from the service and stored as [`openapi.yaml`](./openapi.yaml).
- the backend aggregate OpenAPI file is later merged from service-level specs and used by the frontend for generated client code
- Tests live in `BookingService.Tests`.
- CI quality for backend services runs through `car-rental-services/.github/workflows/ci.yml`.
- The service follows DTO and repository separation, with EF Core migrations applied at startup unless disabled.

## Lessons Learned

### What does not work yet

- Booking creation is eventually consistent because car availability is finalized through RabbitMQ.
- The service depends on external Keycloak and currency-converter availability for full behavior.
- There is no end-to-end workflow test here that exercises booking, car sync, and auth together.

### Experience with technologies

- Split-service ownership is clearer than the earlier combined backend model.
- Keycloak realm roles map cleanly to `app-user` and `app-admin` policies.
- RabbitMQ works well for cross-service synchronization, but it adds eventual-consistency cases that must be documented.

### Experience with AI tools used

- AI assistance helped structure documentation, code organization, and the overall infrastructure story.
- Exact endpoint behavior, authorization rules, and error mapping still had to be verified from source code.
