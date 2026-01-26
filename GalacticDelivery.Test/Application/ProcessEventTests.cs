using GalacticDelivery.Application;
using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Test.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class ProcessEventTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTripRepository _tripRepository;
    private readonly SqliteDriverRepository _driverRepository;
    private readonly SqliteVehicleRepository _vehicleRepository;
    private readonly SqliteRouteRepository _routeRepository;
    private readonly ProcessEvent _useCase;

    public ProcessEventTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);

        _tripRepository = new SqliteTripRepository(_connection);
        _driverRepository = new SqliteDriverRepository(_connection);
        _vehicleRepository = new SqliteVehicleRepository(_connection);
        _routeRepository = new SqliteRouteRepository(_connection);
        _useCase = new ProcessEvent(_tripRepository);
    }

    private static void InitializeDatabase(SqliteConnection connection)
    {
        connection.Execute(Schema.V1);
    }

    private async Task<(Guid routeId, Guid driverId, Guid vehicleId)> CreateTripDependencies()
    {
        var route = new Route(
            Id: null,
            Origin: "Earth",
            Destination: "Mars",
            Checkpoints: new List<Checkpoint> { new("CP1") }
        );
        var createdRoute = await _routeRepository.Create(route);

        var driver = new Driver(Id: null, FirstName: "Alice", LastName: "Smith");
        var createdDriver = await _driverRepository.Create(driver, null);

        var vehicle = new Vehicle(Id: null, RegNumber: "ABC-123");
        var createdVehicle = await _vehicleRepository.Create(vehicle);

        return (createdRoute.Id!.Value, createdDriver.Id!.Value, createdVehicle.Id!.Value);
    }

    private async Task<Guid> CreateTrip(TripStatus status)
    {
        var (routeId, driverId, vehicleId) = await CreateTripDependencies();
        var trip = status == TripStatus.Planned
            ? Trip.Plan(routeId, driverId, vehicleId)
            : new Trip(null, DateTime.UtcNow, routeId, driverId, vehicleId, status, []);
        var created = await _tripRepository.Create(trip);
        return created.Id!.Value;
    }

    private async Task<IReadOnlyList<EventType>> FetchEventTypes(Guid tripId)
    {
        const string sql = """
                           SELECT Type
                           FROM Events
                           WHERE TripId IS @TripId
                           """;
        var rows = await _connection.QueryAsync<string>(sql, new { TripId = tripId.ToString() });
        return rows.Select(Enum.Parse<EventType>).ToList();
    }

    [Fact]
    public async Task Execute_ShouldPersistEventAndUpdateTripStatus_WhenTransitionIsValid()
    {
        var tripId = await CreateTrip(TripStatus.Planned);
        var command = new ProcessEventCommand(tripId, EventType.TripStarted, "start");

        await _useCase.Execute(command);

        var events = await FetchEventTypes(tripId);
        var trip = await _tripRepository.Fetch(tripId);

        Assert.Single(events);
        Assert.Equal(EventType.TripStarted, events[0]);
        Assert.Equal(TripStatus.InProgress, trip.Status);
    }

    [Fact]
    public async Task Execute_ShouldNotPersistEvent_WhenTransitionIsInvalid()
    {
        var tripId = await CreateTrip(TripStatus.Finished);
        var command = new ProcessEventCommand(tripId, EventType.TripStarted, "invalid");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.Execute(command));

        var events = await FetchEventTypes(tripId);
        var trip = await _tripRepository.Fetch(tripId);

        Assert.Empty(events);
        Assert.Equal(TripStatus.Finished, trip.Status);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
