# GalacticDelivery

Backend service for managing drivers, vehicles, routes, trips, and trip events in the Great Galactic Delivery Race domain.

## Installation

Prerequisites:
- .NET SDK 9.0
- (Optional) Docker if you want to build the container image via `compose.yaml`

Steps:
1. Restore dependencies:
   ```bash
   dotnet restore
   ```
2. Run the API:
   ```bash
   dotnet run --project GalacticDelivery.Api.Web
   ```

By default the API uses a SQLite database at `identifier.sqlite` (configured in
`GalacticDelivery.Api.Web/appsettings.json`). Schema creation and seed data are
applied automatically on startup.

The development profile listens on `http://localhost:5114`.

## Docker

Build and run the API with Docker Compose:
```bash
docker compose up --build
```

The API will be available at `http://localhost:5114`. The SQLite database is
stored in `identifier.sqlite` on the host and mounted into the container.

## Tests

Unit tests:
```bash
dotnet test
```

Load tests (NBomber):
1. Start the API (see Installation).
2. Run:
   ```bash
   dotnet run --project GalacticDelivery.LoadTest
   ```

The load test targets `http://localhost:5114`. If you change the API port,
update `GalacticDelivery.LoadTest/Program.cs` accordingly.

## CI

Tests are executed in GitHub Actions on every push and pull request.

## Architecture

This solution follows a clean architecture style with explicit layers:
- `GalacticDelivery.Domain`: core domain model (Trips, Drivers, Vehicles, Routes, Events) and
  domain rules.
- `GalacticDelivery.Application`: use cases and orchestration (planning trips, processing events,
  reporting projections).
- `GalacticDelivery.Infrastructure`: persistence implementations (SQLite repositories using Dapper)
  and transaction handling.
- `GalacticDelivery.Db`: schema and seed data definitions.
- `GalacticDelivery.Api.Web`: HTTP API (minimal API endpoints, OpenAPI in development).
- `GalacticDelivery.Test`: unit and integration-style tests (xUnit + Moq).
- `GalacticDelivery.LoadTest`: NBomber scenario that exercises the trip flow end-to-end.

The API composes the application services and repositories, initializes the SQLite schema,
and exposes endpoints for managing trips and ingesting events.
