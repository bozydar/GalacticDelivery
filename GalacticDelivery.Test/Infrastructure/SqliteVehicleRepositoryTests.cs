using GalacticDelivery.Db;
using GalacticDelivery.Domain;
using GalacticDelivery.Infrastructure;

namespace GalacticDelivery.Test.Infrastructure;

using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

public sealed class SqliteVehicleRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteVehicleRepository _repository;

    public SqliteVehicleRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);
        _repository = new SqliteVehicleRepository(_connection);
    }

    private static void InitializeDatabase(SqliteConnection connection)
    {
        connection.Execute(Schema.V1);
    }

    [Fact]
    public async Task Create_ShouldInsertVehicle()
    {
        var vehicle = new Vehicle(Id: null, RegNumber: "ABC-123");

        var created = await _repository.Create(vehicle);

        Assert.NotNull(created.Id);
        Assert.Equal(vehicle.RegNumber, created.RegNumber);
    }

    [Fact]
    public async Task Fetch_ShouldReturnVehicle_WhenExists()
    {
        var vehicle = new Vehicle(Id: null, RegNumber: "ABC-123", CurrentTripId: Guid.NewGuid());

        var created = await _repository.Create(vehicle);
        var fetched = await _repository.Fetch(created.Id!.Value);

        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.RegNumber, fetched.RegNumber);
        Assert.NotNull(created.CurrentTripId);
    }

    [Fact]
    public async Task Fetch_ShouldThrow_WhenVehicleDoesNotExist()
    {
        var nonExistingId = Guid.NewGuid();

        Assert.Null(await _repository.Fetch(nonExistingId));
    }

    [Fact]
    public async Task FetchAllFree_ShouldReturnVehicles_WhenVehiclesAreNotOnTrip()
    {
        var freeVehicle = await _repository.Create(new Vehicle(Id: null, RegNumber: "ABC-123"));
        await _repository.Create(new Vehicle(Id: null, RegNumber: "BLOCKED", CurrentTripId: Guid.NewGuid()));

        var ids = (await _repository.FetchAllFree()).ToList();
        Assert.Equal(freeVehicle.Id, ids.First());
        Assert.Single(ids);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
