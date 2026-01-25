using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Test.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class SqliteEventRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteEventRepository _repository;
    private readonly SqliteDriverRepository _driverRepository;
    private readonly SqliteVehicleRepository _vehicleRepository;
    private readonly SqliteRouteRepository _routeRepository;
    private readonly SqliteTripRepository _tripRepository;

    public SqliteEventRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);
        _repository = new SqliteEventRepository(_connection);
        _driverRepository = new SqliteDriverRepository(_connection);
        _vehicleRepository = new SqliteVehicleRepository(_connection);
        _routeRepository = new SqliteRouteRepository(_connection);
        _tripRepository = new SqliteTripRepository(_connection);
    }

    private static void InitializeDatabase(SqliteConnection connection)
    {
        connection.Execute(Schema.V1);
    }

    private async Task<Guid> CreateTrip()
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

        var trip = new Trip(
            id: null,
            createdAt: DateTime.UtcNow,
            routeId: createdRoute.Id!.Value,
            driverId: createdDriver.Id!.Value,
            vehicleId: createdVehicle.Id!.Value,
            status: TripStatus.Planned
        );

        var createdTrip = await _tripRepository.Create(trip);
        return createdTrip.Id!.Value;
    }

    [Fact]
    public async Task Create_ShouldInsertEvent()
    {
        var tripId = await CreateTrip();
        var @event = new Event(
            Id: null,
            TripId: tripId,
            CreatedAt: DateTime.UtcNow,
            Type: EventType.TripStarted,
            Payload: "start"
        );

        var created = await _repository.Create(@event, null);

        Assert.NotNull(created.Id);
        Assert.Equal(@event.TripId, created.TripId);
        Assert.Equal(@event.Type, created.Type);
        Assert.Equal(@event.Payload, created.Payload);
    }

    [Fact]
    public async Task FetchByTripId_ShouldReturnEventsForTrip()
    {
        var tripId = await CreateTrip();

        var event1 = await _repository.Create(new Event(
            Id: null,
            TripId: tripId,
            CreatedAt: DateTime.UtcNow,
            Type: EventType.TripStarted,
            Payload: "start"
        ), null);

        var event2 = await _repository.Create(new Event(
            Id: null,
            TripId: tripId,
            CreatedAt: DateTime.UtcNow.AddMinutes(1),
            Type: EventType.CheckpointPassed,
            Payload: "cp1"
        ), null);

        var events = (await _repository.FetchByTripId(tripId)).ToList();

        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Id == event1.Id);
        Assert.Contains(events, e => e.Id == event2.Id);
    }

    [Fact]
    public async Task FetchByTripId_ShouldReturnEmpty_WhenNoEvents()
    {
        var tripId = await CreateTrip();

        var events = await _repository.FetchByTripId(tripId);

        Assert.Empty(events);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
