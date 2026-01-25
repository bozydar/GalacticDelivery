using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Test.Infrastructure;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class SqliteTripRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTripRepository _repository;
    private readonly SqliteDriverRepository _driverRepository;
    private readonly SqliteVehicleRepository _vehicleRepository;
    private readonly SqliteRouteRepository _routeRepository;

    public SqliteTripRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);
        _repository = new SqliteTripRepository(_connection);
        _driverRepository = new SqliteDriverRepository(_connection);
        _vehicleRepository = new SqliteVehicleRepository(_connection);
        _routeRepository = new SqliteRouteRepository(_connection);
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

    [Fact]
    public async Task Create_ShouldInsertTrip()
    {
        var (routeId, driverId, vehicleId) = await CreateTripDependencies();
        var trip = new Trip(
            id: null,
            createdAt: DateTime.UtcNow,
            routeId: routeId,
            driverId: driverId,
            vehicleId: vehicleId,
            status: TripStatus.Planned
        );

        var created = await _repository.Create(trip);

        Assert.NotNull(created.Id);
        Assert.Equal(trip.CreatedAt, created.CreatedAt);
        Assert.Equal(trip.RouteId, created.RouteId);
        Assert.Equal(trip.DriverId, created.DriverId);
        Assert.Equal(trip.VehicleId, created.VehicleId);
        Assert.Equal(trip.Status, created.Status);
    }

    [Fact]
    public async Task Fetch_ShouldReturnTrip_WhenExists()
    {
        var (routeId, driverId, vehicleId) = await CreateTripDependencies();
        var trip = new Trip(
            id: null,
            createdAt: DateTime.UtcNow,
            routeId: routeId,
            driverId: driverId,
            vehicleId: vehicleId,
            status: TripStatus.InProgress
        );

        var created = await _repository.Create(trip);
        var fetched = await _repository.Fetch(created.Id!.Value);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.CreatedAt, fetched.CreatedAt);
        Assert.Equal(created.RouteId, fetched.RouteId);
        Assert.Equal(created.DriverId, fetched.DriverId);
        Assert.Equal(created.VehicleId, fetched.VehicleId);
        Assert.Equal(created.Status, fetched.Status);
    }

    [Fact]
    public async Task Fetch_ShouldThrow_WhenTripDoesNotExist()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => { await _repository.Fetch(nonExistingId); });
    }

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        var (routeId, driverId, vehicleId) = await CreateTripDependencies();
        var trip = new Trip(
            id: null,
            createdAt: DateTime.UtcNow,
            routeId: routeId,
            driverId: driverId,
            vehicleId: vehicleId,
            status: TripStatus.Planned
        );

        var created = await _repository.Create(trip);
        var (newRouteId, newDriverId, newVehicleId) = await CreateTripDependencies();
        var updated = new Trip(
            id: created.Id,
            createdAt: DateTime.UtcNow,
            routeId: newRouteId,
            driverId: newDriverId,
            vehicleId: newVehicleId,
            status: TripStatus.Finished
        );

        await _repository.Update(updated);
        var fetched = await _repository.Fetch(created.Id!.Value);

        Assert.Equal(updated.Id, fetched.Id);
        Assert.Equal(updated.RouteId, fetched.RouteId);
        Assert.Equal(updated.DriverId, fetched.DriverId);
        Assert.Equal(updated.VehicleId, fetched.VehicleId);
        Assert.Equal(updated.Status, fetched.Status);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
