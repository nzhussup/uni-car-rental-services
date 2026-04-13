# CarRentalService API Documentation

## Overview

`CarRentalService` is the main business API for the car rental platform.
It manages cars, bookings, and role-based access rules.

## Technologies

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + SQL Server
- Keycloak JWT authentication and authorization
- Swagger/OpenAPI
- AutoMapper
- SOAP client integration with `CurrencyConverterService`

## API Endpoints

### Cars

- `GET /api/cars`
- `GET /api/cars/{id}`
- `POST /api/cars` (admin)
- `PUT /api/cars/{id}` (admin)
- `DELETE /api/cars/{id}` (admin)
- `PATCH /api/cars/{id}/status` (admin)

### Bookings

- `GET /api/booking` (admin)
- `GET /api/booking/user` (user)
- `GET /api/booking/{id}`
- `POST /api/booking` (user)
- `DELETE /api/booking/{id}` (admin)
- `PATCH /api/booking/{id}/status` (admin)
- `PATCH /api/booking/{id}/cancel` (user)

## Authorization Model

- `app-user` role: user booking operations
- `app-admin` role: administrative operations

Policies are enforced at controller action level.

## Data Model

```text
Car (1) -------- (N) Booking

Car
- Id: int
- Make: string
- Model: string
- Year: int
- PriceInUsd: decimal(18,2)
- Status: CarStatus

Booking
- Id: int
- CarId: int (FK -> Car.Id)
- UserId: Guid (Keycloak subject)
- BookingDate: datetime
- PickupDate: datetime
- DropoffDate: datetime
- TotalCostInUsd: decimal(18,2)
- Status: BookingStatus
```

### Enums

```text
CarStatus:
- Available = 0
- Maintenance = 1

BookingStatus:
- Booked
- Canceled
- PickedUp
- ReturnLate
- Completed
```

## Request and Response Shapes

- List responses: `QueryResponse<T> { totalElements, elements[] }`
- Car response wraps price as `{ amount, currency }`
- Booking response wraps total cost as `{ amount, currency }`

## Business Rules

- Pagination required (`Skip`, `Take`, max `Take=50`)
- Date-filtered car queries exclude overlapping active bookings
- Booking creation prevents overlapping bookings for same car
- Cars in `Maintenance` cannot be booked
- Booking status transitions are constrained (for example completed bookings cannot be modified)

## Error Handling

Global exception middleware maps domain exceptions into standardized HTTP Problem Details responses.

## Quality Notes

- DTO validation via data annotations
- Unit tests for core services:
- `CarServiceTests`
- `BookingServiceTests`
- `UserServiceTests`
- `ExtCurrencyConvertServiceTests`
