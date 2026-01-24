using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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


record Route(Guid Id, string StartPoint, string EndPoint);

record CreateTrip(Guid RouteId);

record Trip(Guid TripId, Guid RouteId);

record Vehicle(Guid VehicleId, string RegNumber);

record Driver(Guid DriverId, string FirstName, string LastName);

enum EventType
{
    TripStarted,
    TripCompleted,
    CheckpointPassed,
    CustomEvent
};


record CreateEvent(Guid EventId, EventType EventType, string Description);