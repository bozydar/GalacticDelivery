using System.Net.Http;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using GalacticDelivery.LoadTest;

var handler = new HttpClientHandler
{
    UseCookies = false
};
var httpClient = new HttpClient(handler)
{
    BaseAddress = new Uri("http://localhost:5114")
};

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
Http.GlobalJsonSerializerOptions = jsonOptions;

var checkpointNames = new[]
{
    "Aurora Gate", "Quasar Ridge", "Photon Belt", "Ion Reef", "Nebula Drift",
    "Graviton Step", "Starlight Sluice", "Comet Trail", "Void Crossing", "Solar Wake"
};

var scenario = Scenario.Create("trip_flow", async context =>
    {
        var (routesOk, routes, routesError) = await GetGuidList(httpClient, "/api/routes/all");
        if (!routesOk)
        {
            return Fail(routesError);
        }

        var (driversOk, drivers, driversError) = await GetGuidList(httpClient, "/api/drivers/free");
        if (!driversOk)
        {
            return Fail(driversError);
        }

        var (vehiclesOk, vehicles, vehiclesError) = await GetGuidList(httpClient, "/api/vehicles/free");
        if (!vehiclesOk)
        {
            return Fail(vehiclesError);
        }

        if (routes.Count == 0 || drivers.Count == 0 || vehicles.Count == 0)
        {
            return Fail("No free resources available");
        }

        var routeId = routes[Random.Shared.Next(routes.Count)];
        var driverId = drivers[Random.Shared.Next(drivers.Count)];
        var vehicleId = vehicles[Random.Shared.Next(vehicles.Count)];

        var (tripOk, trip, tripError) = await CreateTrip(httpClient, routeId, driverId, vehicleId);
        if (!tripOk || trip is null)
        {
            return Fail(tripError);
        }

        var tripId = trip.TripId;

        var (startedOk, startedError) = await EmitEvent(httpClient, tripId, "TripStarted", "Ignition green");
        if (!startedOk)
        {
            return Fail(startedError);
        }

        var checkpoint1 = checkpointNames[Random.Shared.Next(checkpointNames.Length)];
        var checkpoint2 = checkpointNames[Random.Shared.Next(checkpointNames.Length)];

        var (checkpointOk, checkpointError) = await EmitEvent(httpClient, tripId, "CheckpointPassed", checkpoint1);
        if (!checkpointOk)
        {
            return Fail(checkpointError);
        }

        (checkpointOk, checkpointError) = await EmitEvent(httpClient, tripId, "CheckpointPassed", checkpoint2);
        if (!checkpointOk)
        {
            return Fail(checkpointError);
        }

        var accidentCount = Random.Shared.Next(0, 3);
        for (var i = 0; i < accidentCount; i++)
        {
            var (accidentOk, accidentError) = await EmitEvent(httpClient, tripId, "Accident", "Micro-meteor scrape");
            if (!accidentOk)
            {
                return Fail(accidentError);
            }
        }

        var (completedOk, completedError) = await EmitEvent(httpClient, tripId, "TripCompleted", "Docked at target");
        return completedOk ? Response.Ok() : Fail(completedError);
    })
    .WithLoadSimulations(
        Simulation.RampingConstant(copies: 20, during: TimeSpan.FromSeconds(30)),
        Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(30))
    );

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();

IResponse Fail(string error)
{
    return Response.Fail(statusCode: "flow_error", message: error, sizeBytes: 0, customLatencyMs: 0);
}

async Task<(bool Ok, List<Guid> Items, string Error)> GetGuidList(HttpClient client, string url)
{
    var request = Http.CreateRequest("GET", url);
    var (ok, response, error) = await SendRequest(client, request);
    if (!ok || response is null)
    {
        return (false, [], error);
    }

    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return (false, [], $"GET {url} failed: {(int)response.StatusCode} {body}");
    }

    var items = JsonSerializer.Deserialize<List<Guid>>(body, jsonOptions) ?? [];
    return (true, items, string.Empty);
}

async Task<(bool Ok, TripResponse? Trip, string Error)> CreateTrip(
    HttpClient client,
    Guid routeId,
    Guid driverId,
    Guid vehicleId)
{
    var request = Http.CreateRequest("POST", "/api/trip")
        .WithJsonBody(new { routeId, driverId, vehicleId });
    var (ok, response, error) = await SendRequest(client, request);
    if (!ok || response is null)
    {
        return (false, null, error);
    }

    var body = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        return (false, null, $"Plan trip failed: {(int)response.StatusCode} {body}");
    }

    var trip = JsonSerializer.Deserialize<TripResponse>(body, jsonOptions);
    return trip is null
        ? (false, null, "Trip response was empty")
        : (true, trip, string.Empty);
}

async Task<(bool Ok, string Error)> EmitEvent(HttpClient client, Guid tripId, string type, string payload)
{
    var request = Http.CreateRequest("POST", "/queue/event")
        .WithJsonBody(new { tripId, type, payload });
    var (ok, response, error) = await SendRequest(client, request);
    if (!ok || response is null)
    {
        return (false, error);
    }

    var body = await response.Content.ReadAsStringAsync();
    return response.IsSuccessStatusCode
        ? (true, string.Empty)
        : (false, $"Event {type} failed: {(int)response.StatusCode} {body}");
}

async Task<(bool Ok, HttpResponseMessage? Response, string Error)> SendRequest(
    HttpClient client,
    HttpRequestMessage request)
{
    var response = await Http.Send(client, request);
    if (response.IsError)
    {
        return (false, null, response.Message ?? "request_failed");
    }

    if (!NBomberInterop.TryGetPayload(response, out var payload, out var error))
    {
        return (false, null, error);
    }

    return (true, payload, string.Empty);
}

record TripResponse(Guid TripId, Guid RouteId, Guid DriverId, Guid VehicleId);
