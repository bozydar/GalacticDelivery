using GalacticDelivery.Db;
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
        connection.Execute(Schema.V1);
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