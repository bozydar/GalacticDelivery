using System.Data;
using System.Text.Json;
using Dapper;
using GalacticDelivery.Domain;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteRouteRepository : IRouteRepository
{
    private readonly SqliteConnection _connection;

    public SqliteRouteRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<Route> Create(Route route)
    {
        var id = route.Id ?? Guid.NewGuid();

        const string sql = """
                               INSERT INTO Routes (Id, Origin, Destination, Checkpoints)
                               VALUES (@Id, @Origin, @Destination, @Checkpoints);
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            route.Origin,
            route.Destination,
            Checkpoints = JsonSerializer.Serialize(route.Checkpoints)
        });

        return route with { Id = id };
    }

    public async Task<Route> Fetch(Guid routeId)
    {
        const string sql = """
                               SELECT Id, Origin, Destination, Checkpoints
                               FROM Routes
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<RouteRow>(
            sql,
            new { Id = routeId.ToString() }
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Route {routeId} not found");
        }

        return row.ToRoute();
    }

    public async Task<IEnumerable<Route>> FetchAll()
    {
        const string sql = """
                               SELECT Id, Origin, Destination, Checkpoints
                               FROM Routes;
                           """;

        var rows = await _connection.QueryAsync<RouteRow>(
            sql
        );

        return rows.Select(row => row.ToRoute());
    }

    private sealed record RouteRow(
        string Id,
        string Origin,
        string Destination,
        string Checkpoints
    )
    {
        public Route ToRoute()
        {
            var checkpoints = JsonSerializer.Deserialize<List<Checkpoint>>(Checkpoints) ?? [];
            return new Route(Guid.Parse(Id), Origin, Destination, checkpoints);
        }
    }
}