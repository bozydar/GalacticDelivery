using GalacticDelivery.Application;
using GalacticDelivery.Application.Reports;
using GalacticDelivery.Domain;
using Microsoft.AspNetCore.Mvc;

namespace GalacticDelivery.Api.Web.Extensions;

public static class Endpoints
{
    public static void ConfigureApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/drivers/free", async ([FromServices] FetchFreeDrivers fetchFreeDrivers) =>
        {
            var result = await fetchFreeDrivers.Execute();
            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
        });

        app.MapGet("/api/vehicles/free", async ([FromServices] FetchFreeVehicles fetchFreeVehicles) =>
        {
            var result = await fetchFreeVehicles.Execute();
            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
        });

        app.MapGet("/api/routes/all", async ([FromServices] FetchRoutes fetchRoutes) =>
        {
            var result = await fetchRoutes.Execute();
            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
        });

        app.MapPost("/api/trip", async (CreateTrip trip, PlanTrip useCase) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (trip.RouteId == Guid.Empty)
            {
                errors[nameof(CreateTrip.RouteId)] = ["field is required"];
            }

            if (trip.DriverId == Guid.Empty)
            {
                errors[nameof(CreateTrip.DriverId)] = ["field is required"];
            }

            if (trip.VehicleId == Guid.Empty)
            {
                errors[nameof(CreateTrip.VehicleId)] = ["field is required"];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await useCase.Execute(new CreateTripCommand(
                RouteId: trip.RouteId,
                DriverId: trip.DriverId,
                CarId: trip.VehicleId));

            return result.Match(
                onSuccess: id => Results.Ok(new Trip(id, trip.RouteId, trip.DriverId, trip.VehicleId)),
                onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
        });

        app.MapGet("/api/reports/trips-report/{reportId}",
            async (Guid reportId, GetTripReport useCase, CancellationToken cancellationToken) =>
            {
                var report = await useCase.Execute(reportId, cancellationToken);
                return report is null ? Results.NotFound() : Results.Ok(report);
            });

        app.MapPost("/queue/event", async (CreateEvent @event, [FromServices] ProcessEvent useCase) =>
        {
            var command = new ProcessEventCommand(@event.TripId, @event.Type, @event.Payload);
            var result = await useCase.Execute(command);
            return result.Match(
                onSuccess: Results.Ok,
                onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
        });
    }
}

public record CreateTrip(Guid RouteId, Guid DriverId, Guid VehicleId);

public record Trip(Guid TripId, Guid RouteId, Guid DriverId, Guid VehicleId);

public record CreateEvent(Guid TripId, EventType Type, string Payload);

