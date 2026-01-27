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

public sealed class SqliteRouteRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteRouteRepository _repository;

    public SqliteRouteRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        InitializeDatabase(_connection);
        _repository = new SqliteRouteRepository(_connection);
    }

    private static void InitializeDatabase(SqliteConnection connection)
    {
        connection.Execute(Schema.V1);
    }

    [Fact]
    public async Task Create_ShouldInsertRoute()
    {
        var route = new Route(
            Id: null,
            Origin: "Earth",
            Destination: "Mars",
            Checkpoints: new List<Checkpoint> { new("CP1"), new("CP2") }
        );

        var created = await _repository.Create(route);

        Assert.NotNull(created.Id);
        Assert.Equal(route.Origin, created.Origin);
        Assert.Equal(route.Destination, created.Destination);
        Assert.Equal(route.Checkpoints.Count, created.Checkpoints.Count);
    }

    [Fact]
    public async Task Fetch_ShouldReturnRoute_WhenExists()
    {
        var route = new Route(
            Id: null,
            Origin: "Earth",
            Destination: "Mars",
            Checkpoints: new List<Checkpoint> { new("CP1"), new("CP2") }
        );

        var created = await _repository.Create(route);
        var fetched = await _repository.Fetch(created.Id!.Value);

        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Origin, fetched.Origin);
        Assert.Equal(created.Destination, fetched.Destination);
        Assert.Equal(created.Checkpoints.Count, fetched.Checkpoints.Count);
        Assert.Equal(created.Checkpoints[0].Name, fetched.Checkpoints[0].Name);
    }

    [Fact]
    public async Task Fetch_ShouldThrow_WhenRouteDoesNotExist()
    {
        var nonExistingId = Guid.NewGuid();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await _repository.Fetch(nonExistingId);
        });
    }

    [Fact]
    public async Task FetchAll_ShouldReturnRoutes()
    {
        var route1 = new Route(
            Id: null,
            Origin: "Earth",
            Destination: "Mars",
            Checkpoints: new List<Checkpoint> { new("CP1") }
        );
        var route2 = new Route(
            Id: null,
            Origin: "Mars",
            Destination: "Jupiter",
            Checkpoints: new List<Checkpoint> { new("CP2"), new("CP3") }
        );

        var created1 = await _repository.Create(route1);
        var created2 = await _repository.Create(route2);

        var routes = (await _repository.FetchAll()).ToList();

        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, route => route == created1.Id);
        Assert.Contains(routes, route => route == created2.Id);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
