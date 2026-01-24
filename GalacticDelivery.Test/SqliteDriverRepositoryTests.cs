namespace GalacticDelivery.Test;

using System;
using System.Threading.Tasks;
using Dapper;
using Domain;
using Infrastructure;
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
                               LastName TEXT NOT NULL
                           );
                           """);
    }

    [Fact]
    public async Task Create_ShouldInsertDriver()
    {
        var driver = new Driver(Id: null, FirstName: "Alice", LastName: "Smith");

        var created = await _repository.Create(driver);

        Assert.NotNull(created.Id);
        Assert.Equal(driver.FirstName, created.FirstName);
        Assert.Equal(driver.LastName, created.LastName);
    }

    [Fact]
    public async Task Fetch_ShouldReturnDriver_WhenExists()
    {
        var driver = new Driver(Id: null, FirstName: "Alice", LastName: "Smith");

        var created = await _repository.Create(driver);
        var fetched = await _repository.Fetch(created.Id!.Value);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.FirstName, fetched.FirstName);
        Assert.Equal(created.LastName, fetched.LastName);
    }

    [Fact]
    public async Task Fetch_ShouldThrow_WhenDriverDoesNotExist()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () => { await _repository.Fetch(nonExistingId); });
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}