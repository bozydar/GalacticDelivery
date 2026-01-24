using GalacticDelivery.Infrastructure;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Test.Infrastructure;

using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class SqliteDriverRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteDriverRepository _repository;

    public SqliteDriverRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);
        _repository = new SqliteDriverRepository(_connection);
    }

    private void InitializeDatabase(SqliteConnection connection)
    {
        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS Drivers (
                                Id TEXT PRIMARY KEY,
                                FirstName TEXT NOT NULL,
                                LastName TEXT NOT NULL,
                                CurrentTripId TEXT NULL
                           );

                           CREATE TABLE IF NOT EXISTS Vehicles (
                                Id TEXT PRIMARY KEY,
                                RegNumber TEXT NOT NULL,
                                CurrentTripId TEXT NULL
                           );

                           CREATE TABLE IF NOT EXISTS Routes (
                                Id TEXT PRIMARY KEY,
                                "Name" TEXT NOT NULL,
                                StartPoint TEXT NOT NULL,
                                EndPoint TEXT NOT NULL
                           );

                           CREATE TABLE IF NOT EXISTS Trips (
                               Id TEXT PRIMARY KEY,
                               RouteId TEXT NOT NULL,
                               VehicleId TEXT NOT NULL,
                               DriverId TEXT NOT NULL,
                               Status TEXT NOT NULL,
                               FOREIGN KEY (RouteId) REFERENCES Routes(Id),
                               FOREIGN KEY (VehicleId) REFERENCES Routes(Id),
                               FOREIGN KEY (DriverId) REFERENCES Routes(Id)
                           );

                           CREATE TABLE IF NOT EXISTS Events (
                               Id TEXT PRIMARY KEY,
                               CreatedAt DATETIME NOT NULL,
                               TripId TEXT NOT NULL,
                               EventType TEXT NOT NULL,
                               Description TEXT NOT NULL,
                               FOREIGN KEY (TripId) REFERENCES Trips(Id)
                           );
                           """);
    }

    [Fact]
    public async Task Create_ShouldInsertDriver()
    {
        var driver = new Driver(Id: null, FirstName: "Alice", LastName: "Smith");

        var created = await _repository.Create(driver, null);

        Assert.NotNull(created.Id);
        Assert.Equal(driver.FirstName, created.FirstName);
        Assert.Equal(driver.LastName, created.LastName);
    }

    [Fact]
    public async Task Fetch_ShouldReturnDriver_WhenExists()
    {
        var driver = new Driver(Id: null, FirstName: "Alice", LastName: "Smith", CurrentTripId: Guid.NewGuid());

        var created = await _repository.Create(driver, null);
        var fetched = await _repository.Fetch(created.Id!.Value);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.FirstName, fetched.FirstName);
        Assert.Equal(created.LastName, fetched.LastName);
        Assert.NotNull(created.CurrentTripId);
    }

    [Fact]
    public async Task Fetch_ShouldThrow_WhenDriverDoesNotExist()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => { await _repository.Fetch(nonExistingId); });
    }

    [Fact]
    public async Task FetchAllFree_ShouldReturnDrivers_WhenDriversAreNotOnTrip()
    {
        var freeDriver = await _repository.Create(new Driver(Id: null, FirstName: "Alice", LastName: "Smith"), null);
        await _repository.Create(
            new Driver(Id: null, FirstName: "Bob", LastName: "Blocked", CurrentTripId: Guid.NewGuid()), null);

        var ids = (await _repository.FetchAllFree()).ToList();
        Assert.Equal(freeDriver.Id, ids.First());
        Assert.Single(ids);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}