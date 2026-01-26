using GalacticDelivery.Application;
using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Test.Application;

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Moq;
using Xunit;

public sealed class PlanTripTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTripRepository _tripRepository;
    private readonly SqliteDriverRepository _driverRepository;
    private readonly SqliteVehicleRepository _vehicleRepository;
    private readonly SqliteRouteRepository _routeRepository;
    private readonly PlanTrip _planTrip;

    public PlanTripTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);

        _tripRepository = new SqliteTripRepository(_connection);
        _driverRepository = new SqliteDriverRepository(_connection);
        _vehicleRepository = new SqliteVehicleRepository(_connection);
        _routeRepository = new SqliteRouteRepository(_connection);
        var transactionManager = new SqliteTransactionManager(_connection);
        _planTrip = new PlanTrip(
            _tripRepository,
            _driverRepository,
            _vehicleRepository,
            transactionManager);
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

    private async Task<Guid?> FetchDriverCurrentTripId(Guid driverId)
    {
        const string sql = """
                           SELECT CurrentTripId
                           FROM Drivers
                           WHERE Id = @Id
                           """;
        var result = await _connection.QuerySingleOrDefaultAsync<string?>(
            sql,
            new { Id = driverId.ToString() }
        );
        return StringTools.MaybeGuid(result);
    }

    private async Task<Guid?> FetchVehicleCurrentTripId(Guid vehicleId)
    {
        const string sql = """
                           SELECT CurrentTripId
                           FROM Vehicles
                           WHERE Id = @Id
                           """;
        var result = await _connection.QuerySingleOrDefaultAsync<string?>(
            sql,
            new { Id = vehicleId.ToString() }
        );
        return StringTools.MaybeGuid(result);
    }

    private async Task<int> FetchTripCount()
    {
        const string sql = "SELECT COUNT(*) FROM Trips";
        return await _connection.ExecuteScalarAsync<int>(sql);
    }

    [Fact]
    public async Task Execute_ShouldCreateTripAndAssignDriverAndVehicle()
    {
        var (routeId, driverId, vehicleId) = await CreateTripDependencies();
        var command = new CreateTripCommand(routeId, driverId, vehicleId);

        var tripId = await _planTrip.Execute(command);

        var trip = await _tripRepository.Fetch(tripId);
        Assert.Equal(routeId, trip.RouteId);
        Assert.Equal(driverId, trip.DriverId);
        Assert.Equal(vehicleId, trip.VehicleId);
        Assert.Equal(TripStatus.Planned, trip.Status);

        var driverTripId = await FetchDriverCurrentTripId(driverId);
        var vehicleTripId = await FetchVehicleCurrentTripId(vehicleId);

        Assert.Equal(tripId.ToString(), driverTripId.ToString());
        Assert.Equal(tripId.ToString(), vehicleTripId.ToString());
    }

    [Fact]
    public async Task Execute_ShouldRollback_WhenVehicleUpdateFails()
    {
        var (routeId, driverId, vehicleId) = await CreateTripDependencies();
        var vehicle = await _vehicleRepository.Fetch(vehicleId);
        var vehicleRepository = new Mock<IVehicleRepository>();
        vehicleRepository
            .Setup(repo => repo.Fetch(vehicleId, It.IsAny<IDbTransaction?>()))
            .ReturnsAsync(vehicle);
        vehicleRepository
            .Setup(repo => repo.Update(It.IsAny<Vehicle>(), It.IsAny<IDbTransaction?>()))
            .ThrowsAsync(new SqliteException("vehicle update blocked", 1));
        var createTrip = new PlanTrip(
            _tripRepository,
            _driverRepository,
            vehicleRepository.Object,
            new SqliteTransactionManager(_connection));

        var command = new CreateTripCommand(routeId, driverId, vehicleId);

        await Assert.ThrowsAsync<SqliteException>(() => createTrip.Execute(command));

        var tripCount = await FetchTripCount();
        var driverTripId = await FetchDriverCurrentTripId(driverId);
        var vehicleTripId = await FetchVehicleCurrentTripId(vehicleId);

        Assert.Equal(0, tripCount);
        Assert.Null(driverTripId);
        Assert.Null(vehicleTripId);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}