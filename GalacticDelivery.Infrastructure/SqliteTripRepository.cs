using System.Data;
using System.Data.Common;
using Dapper;
using GalacticDelivery.Domain;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteTripRepository : ITripRepository
{
    private readonly SqliteConnection _connection;

    public SqliteTripRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<Trip> Create(Trip trip, DbTransaction? transaction = null)
    {
        const string sql = """
                               INSERT INTO Trips (Id, RouteId, VehicleId, DriverId, Status)
                               VALUES (@Id, @RouteId, @VehicleId, @DriverId, @Status);
                           """;

        var id = Guid.NewGuid();
        await _connection.ExecuteAsync(sql, new
        {
            Id = id,
            trip.RouteId,
            trip.VehicleId,
            trip.DriverId,
            Status = trip.Status.ToString()
        }, transaction: transaction);

        return trip with { Id = id };
    }

    public async Task<Trip> Update(Trip trip, DbTransaction? transaction = null)
    {
        const string sql = """
                               UPDATE Trips SET (RouteId = @RouteId, VehicleId = @VehicleId, DriverId = @DriverId, Status = @Status)
                               WHERE Id = @Id;
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            trip.Id,
            trip.RouteId,
            trip.VehicleId,
            trip.DriverId,
            Status = trip.Status.ToString()
        }, transaction: transaction);

        return trip;
    }

    public Task<Trip> Fetch(Guid tripId)
    {
        throw new NotImplementedException();
    }
}