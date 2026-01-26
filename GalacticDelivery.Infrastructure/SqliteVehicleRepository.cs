using System.Data;
using Dapper;
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

    public async Task<Vehicle> Create(Vehicle Vehicle)
    {
        var id = Vehicle.Id ?? Guid.NewGuid();

        const string sql = """
                               INSERT INTO Vehicles (Id, RegNumber, CurrentTripId)
                               VALUES (@Id, @RegNumber, @CurrentTripId);
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            Vehicle.RegNumber,
            Vehicle.CurrentTripId
        });

        return Vehicle with { Id = id };
    }

    public async Task<Vehicle> Update(Vehicle Vehicle, IDbTransaction? transaction = null)
    {
        var id = Vehicle.Id ?? Guid.NewGuid();

        const string sql = """
                               UPDATE Vehicles SET RegNumber = @RegNumber, CurrentTripId = @CurrentTripId
                               WHERE Id = @Id;
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            Vehicle.RegNumber,
            Vehicle.CurrentTripId
        }, transaction: transaction);

        return Vehicle with { Id = id };
    }

    public async Task<Vehicle> Fetch(Guid VehicleId)
    {
        const string sql = """
                               SELECT Id, RegNumber, CurrentTripId
                               FROM Vehicles
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<VehicleRow>(
            sql,
            new { Id = VehicleId.ToString() }
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Vehicle {VehicleId} not found");
        }

        return new Vehicle(Guid.Parse(row.Id), row.RegNumber, row.CurrentTripId);
    }

    private sealed record VehicleRow(
        string Id,
        string RegNumber,
        Guid? CurrentTripId
    );
}
