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

builder.Services.AddOpenApi();

ConfigureLogging(builder);
ConfigureDatabaseConnection(builder);
RegisterRepositories(builder);
ConfigureJsonSerializationOptions(builder);

var app = builder.Build();
SetupOpenApi(app);
InitializeDatabaseSchema(app);
SetCorrelationIdMiddleware(app);
UseHttpsRedirectionInDevelopment(app);

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

app.MapGet("/api/reports/trips-report/{reportId}", async (Guid reportId, GetTripReport useCase, CancellationToken cancellationToken) =>
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

app.Run();

void ConfigureLogging(WebApplicationBuilder webApplicationBuilder)
{
    webApplicationBuilder.Logging.ClearProviders();
    if (webApplicationBuilder.Environment.IsDevelopment())
    {
        webApplicationBuilder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
    }
    else
    {
        webApplicationBuilder.Logging.AddJsonConsole();
    }
}

void ConfigureDatabaseConnection(WebApplicationBuilder builder1)
{
    var connectionString = builder1.Configuration.GetConnectionString("Sqlite") ?? "Data Source=:memory:";
    builder1.Services.AddScoped(_ =>
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        command.ExecuteNonQuery();
        return connection;
    });
}

void RegisterRepositories(WebApplicationBuilder webApplicationBuilder1)
{
    webApplicationBuilder1.Services.AddScoped<IDriverRepository, SqliteDriverRepository>();
    webApplicationBuilder1.Services.AddScoped<IVehicleRepository, SqliteVehicleRepository>();
    webApplicationBuilder1.Services.AddScoped<IRouteRepository, SqliteRouteRepository>();
    webApplicationBuilder1.Services.AddScoped<ITripRepository, SqliteTripRepository>();
    webApplicationBuilder1.Services.AddScoped<ITransactionManager, SqliteTransactionManager>();
    webApplicationBuilder1.Services.AddScoped<ITripReportRepository, SqliteTripReportRepository>();
    webApplicationBuilder1.Services.AddScoped<ITripReportProjection, TripReportProjection>();
    webApplicationBuilder1.Services.AddScoped<FetchFreeDrivers>();
    webApplicationBuilder1.Services.AddScoped<FetchFreeVehicles>();
    webApplicationBuilder1.Services.AddScoped<FetchRoutes>();
    webApplicationBuilder1.Services.AddScoped<GetTripReport>();
    webApplicationBuilder1.Services.AddScoped<PlanTrip>();
    webApplicationBuilder1.Services.AddScoped<ProcessEvent>();
}

void ConfigureJsonSerializationOptions(WebApplicationBuilder builder2)
{
    builder2.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });
}

void SetupOpenApi(WebApplication webApplication)
{
    if (webApplication.Environment.IsDevelopment())
    {
        webApplication.MapOpenApi();
    }
}

void InitializeDatabaseSchema(WebApplication app1)
{
    using (var scope = app1.Services.CreateScope())
    {
        var connection = scope.ServiceProvider.GetRequiredService<SqliteConnection>();
        using var command = connection.CreateCommand();
        command.CommandText = Schema.V1 + Schema.Seed;
        command.ExecuteNonQuery();
    }
}

void SetCorrelationIdMiddleware(WebApplication webApplication1)
{
    webApplication1.Use(async (context, next) =>
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
}

void UseHttpsRedirectionInDevelopment(WebApplication app2)
{
    if (!app2.Environment.IsDevelopment())
    {
        app2.UseHttpsRedirection();
    }
}


internal record CreateTrip(Guid RouteId, Guid DriverId, Guid VehicleId);

internal record Trip(Guid TripId, Guid RouteId, Guid DriverId, Guid VehicleId);

internal record CreateEvent(Guid TripId, EventType Type, string Payload);
