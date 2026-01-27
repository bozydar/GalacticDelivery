using GalacticDelivery.Application.Reports;
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

public sealed class TripReportProjectionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTripReportRepository _reportRepository;
    private readonly SqliteTripRepository _tripRepository;
    private readonly SqliteRouteRepository _routeRepository;
    private readonly SqliteDriverRepository _driverRepository;
    private readonly SqliteVehicleRepository _vehicleRepository;
    private readonly TripReportProjection _projection;

    public TripReportProjectionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _connection.Execute(Schema.V1);

        _reportRepository = new SqliteTripReportRepository(_connection);
        _tripRepository = new SqliteTripRepository(_connection);
        _routeRepository = new SqliteRouteRepository(_connection);
        _driverRepository = new SqliteDriverRepository(_connection);
        _vehicleRepository = new SqliteVehicleRepository(_connection);

        _projection = new TripReportProjection(
            _reportRepository,
            _tripRepository,
            _routeRepository,
            _driverRepository,
            _vehicleRepository);
    }

    private async Task<(Trip trip, Route route, Driver driver, Vehicle vehicle)> CreateTripDependencies()
    {
        var route = new Route(
            Id: null,
            Origin: "Earth",
            Destination: "Mars",
            Checkpoints: new List<Checkpoint> { new("CP1"), new("CP2") }
        );
        route = await _routeRepository.Create(route);

        var driver = new Driver(Id: null, FirstName: "Han", LastName: "Solo");
        driver = await _driverRepository.Create(driver, null);

        var vehicle = new Vehicle(Id: null, RegNumber: "XR-77");
        vehicle = await _vehicleRepository.Create(vehicle);

        var trip = Trip.Plan(route.Id!.Value, driver.Id!.Value, vehicle.Id!.Value);
        trip = await _tripRepository.Create(trip);

        return (trip, route, driver, vehicle);
    }

    [Fact]
    public async Task Apply_ShouldCreateReport_WhenMissing()
    {
        var (trip, route, driver, vehicle) = await CreateTripDependencies();
        var startedAt = trip.CreatedAt.AddMinutes(5);

        var @event = new Event(
            Id: Guid.NewGuid(),
            TripId: trip.Id!.Value,
            CreatedAt: startedAt,
            Type: EventType.TripStarted,
            Payload: null);

        await _projection.Apply(@event, null);

        var report = await _reportRepository.Fetch(trip.Id!.Value);

        Assert.NotNull(report);
        Assert.Equal(trip.Id, report!.TripId);
        Assert.Equal(trip.CreatedAt, report.CreatedAt);
        Assert.Equal(startedAt, report.StartedAt);
        Assert.Equal(driver.Id, report.DriverId);
        Assert.Equal($"{driver.FirstName} {driver.LastName}", report.DriverName);
        Assert.Equal(vehicle.Id, report.VehicleId);
        Assert.Equal(vehicle.RegNumber, report.VehicleRegistrationNumber);
        Assert.Equal(route.Id, report.RouteId);
        Assert.Equal(route.Origin, report.RouteOrigin);
        Assert.Equal(route.Destination, report.RouteDestination);
        Assert.Equal(route.Checkpoints.Select(c => c.Name).ToList(), report.CheckpointsPlanned);
        Assert.Empty(report.CheckpointsPassed);
        Assert.Single(report.Events);
        Assert.Equal(EventType.TripStarted.ToString(), report.Events[0].Type);
    }

    [Fact]
    public async Task Apply_ShouldAddCheckpoint_AndAvoidDuplicates()
    {
        var (trip, _, _, _) = await CreateTripDependencies();

        var first = new Event(
            Id: Guid.NewGuid(),
            TripId: trip.Id!.Value,
            CreatedAt: trip.CreatedAt.AddMinutes(10),
            Type: EventType.CheckpointPassed,
            Payload: "CP1");

        var duplicate = new Event(
            Id: Guid.NewGuid(),
            TripId: trip.Id!.Value,
            CreatedAt: trip.CreatedAt.AddMinutes(12),
            Type: EventType.CheckpointPassed,
            Payload: "CP1");

        await _projection.Apply(first);
        await _projection.Apply(duplicate);

        var report = await _reportRepository.Fetch(trip.Id!.Value);

        Assert.NotNull(report);
        Assert.Single(report!.CheckpointsPassed);
        Assert.Equal("CP1", report.CheckpointsPassed[0]);
        Assert.Equal(2, report.Events.Count);
    }

    [Fact]
    public async Task Apply_ShouldSetCompletion_AndDuration()
    {
        var (trip, _, _, _) = await CreateTripDependencies();
        var startedAt = trip.CreatedAt.AddMinutes(5);
        var completedAt = trip.CreatedAt.AddMinutes(45);

        var startEvent = new Event(
            Id: Guid.NewGuid(),
            TripId: trip.Id!.Value,
            CreatedAt: startedAt,
            Type: EventType.TripStarted,
            Payload: null);

        var completeEvent = new Event(
            Id: Guid.NewGuid(),
            TripId: trip.Id!.Value,
            CreatedAt: completedAt,
            Type: EventType.TripCompleted,
            Payload: null);

        await _projection.Apply(startEvent);
        await _projection.Apply(completeEvent);

        var report = await _reportRepository.Fetch(trip.Id!.Value);

        Assert.NotNull(report);
        Assert.Equal(startedAt, report!.StartedAt);
        Assert.Equal(completedAt, report.CompletedAt);
        Assert.Equal((long)(completedAt - startedAt).TotalSeconds, report.DurationSeconds);
        Assert.Equal(2, report.Events.Count);
        Assert.Equal(nameof(EventType.TripStarted), report.Events[0].Type);
        Assert.Equal(nameof(EventType.TripCompleted), report.Events[1].Type);
    }

    [Fact]
    public async Task Apply_ShouldIncrementIncidentsCount()
    {
        var (trip, _, _, _) = await CreateTripDependencies();

        var accident = new Event(
            Id: Guid.NewGuid(),
            TripId: trip.Id!.Value,
            CreatedAt: trip.CreatedAt.AddMinutes(7),
            Type: EventType.Accident,
            Payload: "Minor hull damage");

        await _projection.Apply(accident);

        var report = await _reportRepository.Fetch(trip.Id!.Value);

        Assert.NotNull(report);
        Assert.Equal(1, report!.IncidentsCount);
        Assert.Single(report.Events);
        Assert.Equal(nameof(EventType.Accident), report.Events[0].Type);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
