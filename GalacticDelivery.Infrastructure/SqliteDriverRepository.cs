using System.Data;
using Dapper;
using GalacticDelivery.Domain;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteDriverRepository : IDriverRepository
{
    private readonly SqliteConnection _connection;

    public SqliteDriverRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<Driver> Create(Driver driver)
    {
        var id = driver.Id ?? Guid.NewGuid();

        const string sql = """
                               INSERT INTO Drivers (Id, FirstName, LastName)
                               VALUES (@Id, @FirstName, @LastName);
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            driver.FirstName,
            driver.LastName
        });

        return driver with { Id = id };
    }

    public async Task<Driver> Fetch(Guid driverId)
    {
        const string sql = """
                               SELECT Id, FirstName, LastName
                               FROM Drivers
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<DriverRow>(
            sql,
            new { Id = driverId.ToString() }
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Driver {driverId} not found");
        }

        return new Driver(Guid.Parse(row.Id), row.FirstName, row.LastName);
    }

    private sealed record DriverRow(
        string Id,
        string FirstName,
        string LastName
    );
}