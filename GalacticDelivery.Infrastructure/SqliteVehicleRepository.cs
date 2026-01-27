using System.Data;
using System.Data.Common;
using Dapper;
using GalacticDelivery.Common;
using GalacticDelivery.Domain;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteVehicleRepository : IVehicleRepository
{
    private readonly SqliteConnection _connection;

    public SqliteVehicleRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<Vehicle> Create(Vehicle vehicle)
    {
        var id = vehicle.Id ?? Guid.NewGuid();

        const string sql = """
                               INSERT INTO Vehicles (Id, RegNumber, CurrentTripId)
                               VALUES (@Id, @RegNumber, @CurrentTripId);
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            vehicle.RegNumber,
            vehicle.CurrentTripId
        });

        return vehicle with { Id = id };
    }

    public async Task<Vehicle> Update(Vehicle vehicle, DbTransaction? transaction = null)
    {
        var id = vehicle.Id ?? Guid.NewGuid();

        const string sql = """
                               UPDATE Vehicles SET RegNumber = @RegNumber, CurrentTripId = @CurrentTripId
                               WHERE Id = @Id;
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            vehicle.RegNumber,
            vehicle.CurrentTripId
        }, transaction: transaction);

        return vehicle with { Id = id };
    }

    public async Task<Vehicle> Fetch(Guid vehicleId, DbTransaction? transaction = null)
    {
        const string sql = """
                               SELECT Id, RegNumber, CurrentTripId
                               FROM Vehicles
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<VehicleRow>(
            sql,
            new { Id = vehicleId.ToString() },
            transaction: transaction
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Vehicle {vehicleId} not found");
        }

        return new Vehicle(Guid.Parse(row.Id), row.RegNumber, StringTools.MaybeGuid(row.CurrentTripId));
    }

    public async Task<IEnumerable<Guid>> FetchAllFree()
    {
        const string sql = """
                               SELECT Id
                               FROM Vehicles
                               WHERE CurrentTripId IS NULL
                           """;
        var ids = await _connection.QueryAsync<string>(sql);
        return ids.Select(Guid.Parse);
    }

    private sealed record VehicleRow(
        string Id,
        string RegNumber,
        string CurrentTripId
    );
}
