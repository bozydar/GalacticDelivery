using System.Text.Json.Serialization;
using GalacticDelivery.Application;
using GalacticDelivery.Application.Reports;
using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
builder.Services.AddScoped<ITripRepository, SqliteTripRepository>();
builder.Services.AddScoped<ITransactionManager, SqliteTransactionManager>();
builder.Services.AddScoped<FetchFreeDrivers>();
builder.Services.AddScoped<GetTripReport>();
builder.Services.AddScoped<PlanTrip>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var connection = scope.ServiceProvider.GetRequiredService<SqliteConnection>();
    using var command = connection.CreateCommand();
    command.CommandText = Schema.V1;
    command.ExecuteNonQuery();
}

app.MapGet("/api/vehicles", () =>
{

}).WithName("GetVehicles");

app.MapGet("/api/drivers", () =>
{

}).WithName("GetDrivers");

app.MapGet("/api/drivers/free", async (FetchFreeDrivers useCase) =>
    {
        var result = await useCase.Execute();
        return result.Match(
            onSuccess: Results.Ok,
            onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
    })
    .WithName("GetFreeDrivers");

app.MapGet("/api/vehicles/free", async (FetchFreeDrivers useCase) =>
    {
        var result = await useCase.Execute();
        return result.Match(
            onSuccess: Results.Ok,
            onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
    })
    .WithName("GetFreeDrivers");

app.MapGet("/api/routes", () =>
{

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
            onSuccess: id => Results.Ok(new Trip(id, trip.RouteId)),
            onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
    })
    .WithName("PlanTrip");

app.MapGet("/api/reports/trips-report/{reportId}", async (Guid reportId, GetTripReport useCase) =>
{
    var report = await useCase.Execute(reportId);
    return report is null ? Results.NotFound() : Results.Ok(report);
}).WithName("GetTripReport");

app.MapPost("/queue/event", (CreateEvent @event) =>
{

}).WithName("CreateEvent");


app.Run();


record Route(Guid RouteId, string StartPoint, string EndPoint);

record CreateTrip(Guid RouteId, Guid DriverId, Guid VehicleId);

record Trip(Guid TripId, Guid RouteId);

record Vehicle(Guid VehicleId, string RegNumber);

record Driver(Guid DriverId, string FirstName, string LastName);

record CreateEvent(Guid EventId, EventType EventType, string Description);
