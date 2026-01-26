using System.Text.Json.Serialization;
using GalacticDelivery.Application;
using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var sqlitePath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "identifier.sqlite"));
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
builder.Services.AddScoped<FetchFreeDrivers>();

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
    List<Vehicle> result =
    [
        new(Guid.NewGuid(), "ABC 1111"),
        new(Guid.NewGuid(), "DEF 222"),
        new(Guid.NewGuid(), "GHI 333"),
    ];
    return result;
}).WithName("GetVehicles");

app.MapGet("/api/drivers", () =>
{
    List<Driver> result =
    [
        new(Guid.NewGuid(), "Adam", "Adamski"),
        new(Guid.NewGuid(), "Bob", "Builder"),
        new(Guid.NewGuid(), "Cecylia", "Cyckowska"),
    ];
    return result;
}).WithName("GetDrivers");

app.MapGet("/api/drivers/free", async (FetchFreeDrivers useCase) =>
    {
        var result = await useCase.Execute();
        return result.Match(
            onSuccess: ids => Results.Ok(ids),
            onFailure: error => Results.BadRequest(new { error.Code, error.Message }));
    })
    .WithName("GetFreeDrivers");

app.MapGet("/api/routes", () =>
{
    List<Route> result =
    [
        new(Guid.NewGuid(), "Auckland", "Wellington"),
        new(Guid.NewGuid(), "Wellington", "Gisborne"),
        new(Guid.NewGuid(), "Gisborne", "Hamilton"),
        new(Guid.NewGuid(), "Hamilton", "Napier"),
    ];
    return result;
}).WithName("GetRoutes");

app.MapPost("/api/trip", (CreateTrip trip) =>
    {
        if (trip.RouteId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(CreateTrip.RouteId)] = ["field is required"]
            }, statusCode: StatusCodes.Status400BadRequest);
        }

        return Results.Ok(new Trip(Guid.NewGuid(), trip.RouteId));
    })
    .WithName("CreateTrip");

app.MapPost("/queue/event", (CreateEvent @event) =>
{
    Console.WriteLine(@event);
    return Results.Ok();
}).WithName("CreateEvent");


app.Run();


record Route(Guid RouteId, string StartPoint, string EndPoint);

record CreateTrip(Guid RouteId);

record Trip(Guid TripId, Guid RouteId);

record Vehicle(Guid VehicleId, string RegNumber);

record Driver(Guid DriverId, string FirstName, string LastName);

record CreateEvent(Guid EventId, EventType EventType, string Description);
