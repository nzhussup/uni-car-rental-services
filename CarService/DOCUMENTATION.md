# Car Service Documentation

## Overview and Responsibility

`CarService` is the catalog and availability API for vehicles. It stores the car inventory, filters available cars by date range, converts prices through the currency service, and publishes car availability or maintenance events to the booking domain over RabbitMQ.

## Tools and Technologies Used

- ASP.NET Core Web API on .NET
- Entity Framework Core with SQL Server
- Keycloak AuthServices for JWT authentication and admin authorization
- AutoMapper for DTO mapping
- RabbitMQ.Client for asynchronous integration
- Swagger / OpenAPI for contract generation

## API Description

Base route: `/api/cars`

| Method | Route | Purpose | Authorization |
| --- | --- | --- | --- |
| `GET` | `/api/cars` | Search and page through cars | public |
| `GET` | `/api/cars/{id}` | Read one car | public |
| `POST` | `/api/cars` | Create a car | `app-admin` |
| `PUT` | `/api/cars/{id}` | Update a car | `app-admin` |
| `PATCH` | `/api/cars/{id}/status` | Change availability status | `app-admin` |
| `DELETE` | `/api/cars/{id}` | Delete a car and trigger maintenance-style cancellation flow | `app-admin` |

Filter inputs on `GET /api/cars`:

- `CarManufacturer`
- `CarModel`
- `Year`
- `Status`
- `PickupDate`
- `DropoffDate`
- `Skip`
- `Take`
- `targetCurrency`

## Data Model

```text
Car
|- Id
|- Make
|- Model
|- Year
|- Price (stored in USD in backend logic)
|- Status
`- UnavailableDates[*]
   |- BookingId
   |- PickupDate
   `- DropOffDate
```

Availability logic:

- cars can be filtered by metadata only
- if both `PickupDate` and `DropoffDate` are supplied, only `Available` cars without overlapping `UnavailableDates` are returned
- booking reservations are written indirectly from `booking.info` events

## Runtime Wiring and Dependencies

```text
Frontend -> NginxGateway -> CarService
CarService -> SQL Server
CarService -> Keycloak
CarService -> CurrencyConverterService (gRPC client)
CarService -> RabbitMQ exchange: car.*
CarService <- RabbitMQ exchange: booking.*
```

`BookingSubscriber` listens for booking events and updates the car's unavailable date ranges. `MessageProducer` emits:

- `car.info` when a car check succeeds or fails
- `car.maintenance` when a car is deleted or moved into maintenance

RabbitMQ integration details:

- published exchange: `car_exchange`
- published routing keys:
  - `car.info`
  - `car.maintenance`
- published payload types:
  - `CarInfo`
  - `MaintainanceStartInfo`
- subscribed exchange: `booking_exchange`
- subscribed queue: `booking_queue`
- subscribed routing pattern: `booking.*`

Consumed payload type:

- `BookingInfo`
  - `Type = Check` asks the service to reserve availability for a requested booking
  - `Type = Canceled` removes a previously reserved unavailable date range

Event flow:

1. `BookingService` publishes `BookingInfo`
2. `CarService` consumes it from `booking_queue`
3. if the car is available, the service stores an unavailable date range and publishes `CarInfo` with `IsAvailable = true`
4. if the car is unavailable or missing, the service publishes `CarInfo` with `IsAvailable = false`
5. if a booking is canceled, the service removes the related unavailable date range
6. if a car is deleted or put into maintenance, the service publishes `MaintainanceStartInfo`

## Validation and Error Handling

The service relies on DTO binding plus domain rules:

- missing cars return `404`
- admin-only mutations return `403` when the caller lacks `app-admin`
- maintenance changes publish cancellation signals to the booking side
- date overlap filtering uses stored unavailable ranges rather than direct booking queries
- incomplete currency or Keycloak configuration fails startup

`GlobalExceptionHandlerMiddleware` returns JSON Problem Details for:

- `NotFoundException` -> `404`
- `NotAllowedException` -> `403`
- `UserIdNotFoundException` -> `401`
- unexpected exceptions -> `500`

## Code Quality Practices

- OpenAPI is generated into [`openapi.yaml`](./openapi.yaml).
- the backend aggregate OpenAPI file is later merged from service-level specs and used by the frontend for generated client code
- Tests live in `CarService.Tests`.
- CI matrix jobs run quality, test, and build stages for this service.
- The codebase keeps controller, service, DTO, repository, and subscriber concerns separate.

## Lessons Learned

### What does not work yet

- Booking consistency is asynchronous, so immediate reads after booking creation can briefly lag.
- The service still contains TODO notes around the edge case where a booking refers to a missing car.
- There is no dedicated contract or workflow test for the RabbitMQ synchronization path.

### Experience with technologies

- Separating car search from booking orchestration keeps the API boundaries clearer.
- EF Core and repository abstractions were enough for the required filtering and update flows.
- RabbitMQ topic exchanges provide a simple way to decouple inventory changes from booking updates.

### Experience with AI tools used

- AI was useful for structuring modules and brainstorming the split-service architecture.
- Source-level review was still required to document the actual event flow and availability logic correctly.
