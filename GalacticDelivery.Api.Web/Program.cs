using System.Text.Json.Serialization;
using GalacticDelivery.Application;
using GalacticDelivery.Application.Reports;
using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
else
{
    builder.Logging.AddJsonConsole();
}

var connectionString = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=:memory:";
builder.Services.AddScoped(_ =>
{
    var connection = new SqliteConnection(connectionString);
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
    command.ExecuteNonQuery();
    return connection;
});
builder.Services.AddScoped<IDriverRepository, SqliteDriverRepository>();
builder.Services.AddScoped<IVehicleRepository, SqliteVehicleRepository>();
builder.Services.AddScoped<IRouteRepository, SqliteRouteRepository>();
builder.Services.AddScoped<ITripRepository, SqliteTripRepository>();
builder.Services.AddScoped<ITransactionManager, SqliteTransactionManager>();
builder.Services.AddScoped<ITripReportRepository, SqliteTripReportRepository>();
builder.Services.AddScoped<ITripReportProjection, TripReportProjection>();
builder.Services.AddScoped<FetchFreeDrivers>();
builder.Services.AddScoped<FetchFreeVehicles>();
builder.Services.AddScoped<FetchRoutes>();
builder.Services.AddScoped<GetTripReport>();
builder.Services.AddScoped<PlanTrip>();
builder.Services.AddScoped<ProcessEvent>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = context.TraceIdentifier;
    }

    context.Items["CorrelationId"] = correlationId;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        return Task.CompletedTask;
    });

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
    {
        await next();
    }
});

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var correlationId = context.Items["CorrelationId"] as string
            ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.TraceIdentifier;

        if (exceptionHandler?.Error is not null)
        {
            logger.LogError(exceptionHandler.Error, "Unhandled exception for {Method} {Path} ({CorrelationId})",
                context.Request.Method, context.Request.Path, correlationId);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "internal_error", correlationId });
    });
});

// app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var connection = scope.ServiceProvider.GetRequiredService<SqliteConnection>();
    using var command = connection.CreateCommand();
    command.CommandText = Schema.V1 + Schema.Seed;
    command.ExecuteNonQuery();
}

app.MapGet("/api/drivers/free", async ([FromServices] FetchFreeDrivers fetchFreeDrivers) =>
    {
        var result = await fetchFreeDrivers.Execute();
        return result.Match(
            onSuccess: Results.Ok,
            onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
    })
    .WithName("GetFreeDrivers");

app.MapGet("/api/vehicles/free", async ([FromServices] FetchFreeVehicles fetchFreeVehicles) =>
    {
        var result = await fetchFreeVehicles.Execute();
        return result.Match(
            onSuccess: Results.Ok,
            onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
    })
    .WithName("GetFreeVehicles");

app.MapGet("/api/routes/all", async ([FromServices] FetchRoutes fetchRoutes) =>
{
    var result = await fetchRoutes.Execute();
    return result.Match(
        onSuccess: Results.Ok,
        onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
}).WithName("GetRoutes");

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
    })
    .WithName("PlanTrip");

app.MapGet("/api/reports/trips-report/{reportId}", async (Guid reportId, GetTripReport useCase) =>
{
    var report = await useCase.Execute(reportId);
    return report is null ? Results.NotFound() : Results.Ok(report);
}).WithName("GetTripReport");

app.MapPost("/queue/event", async (CreateEvent @event, [FromServices] ProcessEvent useCase) =>
{
    var command = new ProcessEventCommand(@event.TripId, @event.Type, @event.Payload);
    var result = await useCase.Execute(command);
    return result.Match(
        onSuccess: Results.Ok,
        onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
}).WithName("CreateEvent");


app.Run();


record Route(Guid RouteId, string StartPoint, string EndPoint);

record CreateTrip(Guid RouteId, Guid DriverId, Guid VehicleId);

record Trip(Guid TripId, Guid RouteId, Guid DriverId, Guid VehicleId);

record Vehicle(Guid VehicleId, string RegNumber);

record Driver(Guid DriverId, string FirstName, string LastName);

record CreateEvent(Guid TripId, EventType Type, string Payload);
