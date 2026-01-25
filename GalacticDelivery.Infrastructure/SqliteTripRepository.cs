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
                               INSERT INTO Trips (Id, CreatedAt, RouteId, VehicleId, DriverId, Status)
                               VALUES (@Id, @CreatedAt, @RouteId, @VehicleId, @DriverId, @Status);
                           """;

        var id = Guid.NewGuid();
        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            trip.CreatedAt,
            RouteId = trip.RouteId.ToString(),
            VehicleId = trip.VehicleId.ToString(),
            DriverId = trip.DriverId.ToString(),
            Status = trip.Status.ToString()
        }, transaction: transaction);

        return trip with { Id = id };
    }

    public async Task<Trip> Update(Trip trip, DbTransaction? transaction = null)
    {
        const string sql = """
                               UPDATE Trips SET RouteId = @RouteId, VehicleId = @VehicleId, DriverId = @DriverId, Status = @Status
                               WHERE Id = @Id;
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = trip.Id.ToString(),
            RouteId = trip.RouteId.ToString(),
            VehicleId = trip.VehicleId.ToString(),
            DriverId = trip.DriverId.ToString(),
            Status = trip.Status.ToString()
        }, transaction: transaction);

        return trip;
    }

    public async Task<Trip> Fetch(Guid tripId)
    {
        const string sql = """
                               SELECT Id, CreatedAt, RouteId, VehicleId, DriverId, Status
                               FROM Trips
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<TripRow>(
            sql,
            new { Id = tripId.ToString() }
        );

        if (row is null)
        {
            throw new KeyNotFoundException($"Trip {tripId} not found");
        }

        return row.ToTrip();
    }

    private sealed record TripRow(
        string Id,
        string CreatedAt,
        string RouteId,
        string VehicleId,
        string DriverId,
        string Status)
    {
        public Trip ToTrip()
        {
            return new Trip(
                id: Guid.Parse(Id),
                createdAt: DateTime.Parse(CreatedAt),
                routeId: Guid.Parse(RouteId),
                driverId: Guid.Parse(DriverId),
                vehicleId: Guid.Parse(VehicleId),
                status: Enum.Parse<TripStatus>(Status));
        }
    }
}