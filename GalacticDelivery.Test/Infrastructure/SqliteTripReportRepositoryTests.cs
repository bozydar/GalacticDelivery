using GalacticDelivery.Application.Reports;
using GalacticDelivery.Db;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Test.Infrastructure;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class SqliteTripReportRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTripReportRepository _repository;

    public SqliteTripReportRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);
        _repository = new SqliteTripReportRepository(_connection);
    }

    private static void InitializeDatabase(SqliteConnection connection)
    {
        connection.Execute(Schema.V1);
    }

    private static TripReportModel CreateReport(Guid tripId)
    {
        return new TripReportModel(
            TripId: tripId,
            GeneratedAt: new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            CreatedAt: new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            StartedAt: new DateTime(2025, 1, 1, 10, 5, 0, DateTimeKind.Utc),
            CompletedAt: new DateTime(2025, 1, 1, 10, 35, 0, DateTimeKind.Utc),
            DurationSeconds: 1800,
            DriverId: Guid.NewGuid(),
            DriverName: "Han Solo",
            VehicleId: Guid.NewGuid(),
            VehicleRegistrationNumber: "XR-77",
            RouteId: Guid.NewGuid(),
            RouteOrigin: "Mars",
            RouteDestination: "Europa",
            CheckpointsPlanned: new List<string> { "Phobos", "Ceres", "Ganymede" },
            CheckpointsPassed: new List<string> { "Phobos", "Ceres" },
            IncidentsCount: 1,
            Events: []);
    }

    [Fact]
    public async Task Fetch_ShouldReturnNull_WhenReportDoesNotExist()
    {
        var result = await _repository.Fetch(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertReport_ShouldInsertAndFetch()
    {
        var tripId = Guid.NewGuid();
        var report = CreateReport(tripId);

        await _repository.UpsertReport(report);

        var firstEvent = new TripReportEventModel(
            Id: Guid.NewGuid(),
            TripId: tripId,
            CreatedAt: new DateTime(2025, 1, 1, 10, 5, 0, DateTimeKind.Utc),
            Type: "TripStarted",
            Payload: null);
        var secondEvent = new TripReportEventModel(
            Id: Guid.NewGuid(),
            TripId: tripId,
            CreatedAt: new DateTime(2025, 1, 1, 10, 35, 0, DateTimeKind.Utc),
            Type: "TripCompleted",
            Payload: null);

        await _repository.AddReportEvent(secondEvent);
        await _repository.AddReportEvent(firstEvent);

        var fetched = await _repository.Fetch(tripId);

        Assert.NotNull(fetched);
        Assert.Equal(report.TripId, fetched!.TripId);
        Assert.Equal(report.GeneratedAt, fetched.GeneratedAt);
        Assert.Equal(report.CreatedAt, fetched.CreatedAt);
        Assert.Equal(report.StartedAt, fetched.StartedAt);
        Assert.Equal(report.CompletedAt, fetched.CompletedAt);
        Assert.Equal(report.DurationSeconds, fetched.DurationSeconds);
        Assert.Equal(report.DriverId, fetched.DriverId);
        Assert.Equal(report.DriverName, fetched.DriverName);
        Assert.Equal(report.VehicleId, fetched.VehicleId);
        Assert.Equal(report.VehicleRegistrationNumber, fetched.VehicleRegistrationNumber);
        Assert.Equal(report.RouteId, fetched.RouteId);
        Assert.Equal(report.RouteOrigin, fetched.RouteOrigin);
        Assert.Equal(report.RouteDestination, fetched.RouteDestination);
        Assert.Equal(report.CheckpointsPlanned, fetched.CheckpointsPlanned);
        Assert.Equal(report.CheckpointsPassed, fetched.CheckpointsPassed);
        Assert.Equal(report.IncidentsCount, fetched.IncidentsCount);

        Assert.Equal(2, fetched.Events.Count);
        Assert.Equal("TripStarted", fetched.Events[0].Type);
        Assert.Equal("TripCompleted", fetched.Events[1].Type);
    }

    [Fact]
    public async Task UpsertReport_ShouldUpdateExisting()
    {
        var tripId = Guid.NewGuid();
        var report = CreateReport(tripId);

        await _repository.UpsertReport(report);

        var updated = report with
        {
            CompletedAt = new DateTime(2025, 1, 1, 10, 45, 0, DateTimeKind.Utc),
            DurationSeconds = 2400,
            CheckpointsPassed = new List<string> { "Phobos", "Ceres", "Ganymede" },
            IncidentsCount = 2
        };

        await _repository.UpsertReport(updated);

        var fetched = await _repository.Fetch(tripId);

        Assert.NotNull(fetched);
        Assert.Equal(updated.CompletedAt, fetched!.CompletedAt);
        Assert.Equal(updated.DurationSeconds, fetched.DurationSeconds);
        Assert.Equal(updated.CheckpointsPassed, fetched.CheckpointsPassed);
        Assert.Equal(updated.IncidentsCount, fetched.IncidentsCount);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
