using System.Data;
using System.Data.Common;
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

    public async Task<Route> Fetch(Guid routeId, DbTransaction? transaction = null)
    {
        const string sql = """
                               SELECT Id, Origin, Destination, Checkpoints
                               FROM Routes
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<RouteRow>(
            sql,
            new { Id = routeId.ToString() },
            transaction
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Route {routeId} not found");
        }

        return row.ToRoute();
    }

    public async Task<IEnumerable<Guid>> FetchAll()
    {
        const string sql = """
                               SELECT Id
                               FROM Routes;
                           """;

        var ids = await _connection.QueryAsync<string>(sql);
        return ids.Select(Guid.Parse);
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