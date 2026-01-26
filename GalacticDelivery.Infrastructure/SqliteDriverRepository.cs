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

    public async Task<Driver> Create(Driver driver, IDbTransaction? tx)
    {
        var id = driver.Id ?? Guid.NewGuid();

        const string sql = """
                               INSERT INTO Drivers (Id, FirstName, LastName, CurrentTripId)
                               VALUES (@Id, @FirstName, @LastName, @CurrentTripId);
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            driver.FirstName,
            driver.LastName,
            driver.CurrentTripId
        }, transaction: tx);

        return driver with { Id = id };
    }

    public async Task<Driver> Update(Driver driver, IDbTransaction? tx)
    {
        var id = driver.Id ?? Guid.NewGuid();

        const string sql = """
                               UPDATE Drivers SET FirstName = @FirstName, LastName = @LastName, CurrentTripId = @CurrentTripId
                               WHERE Id = @Id;
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            driver.FirstName,
            driver.LastName,
            CurrentTripId = driver.CurrentTripId.ToString()
        }, transaction: tx);

        return driver;
    }

    public async Task<Driver> Fetch(Guid driverId, IDbTransaction? transaction = null)
    {
        const string sql = """
                               SELECT Id, FirstName, LastName, CurrentTripId
                               FROM Drivers
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<DriverRow>(
            sql,
            new { Id = driverId.ToString() },
            transaction: transaction
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Driver {driverId} not found");
        }
        
        return new Driver(Guid.Parse(row.Id), row.FirstName, row.LastName, StringTools.MaybeGuid(row.CurrentTripId));
    }

    public async Task<IEnumerable<Guid>> FetchAllFree()
    {
        const string sql = """
                               SELECT Id
                               FROM Drivers
                               WHERE CurrentTripId IS NULL
                           """;
        var ids = await _connection.QueryAsync<string>(sql);
        return ids.Select(Guid.Parse);
    }

    private sealed record DriverRow(
        string Id,
        string FirstName,
        string LastName,
        string CurrentTripId
    );
}