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
        var events = await SaveEvents(trip.Events, transaction);

        return trip with { Id = id, Events = events.ToList()};
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
        var events = await SaveEvents(trip.Events, transaction);

        return trip with { Events = events.ToList()};
    }

    public async Task<Trip?> Fetch(Guid tripId, DbTransaction? transaction = null)
    {
        const string sql = """
                               SELECT Id, CreatedAt, RouteId, VehicleId, DriverId, Status
                               FROM Trips
                               WHERE Id = @Id;
                           """;

        var row = await _connection.QuerySingleOrDefaultAsync<TripRow>(
            sql,
            new { Id = tripId.ToString() },
            transaction
        );

        return row?.ToTrip();
    }

    private async Task<IEnumerable<Event>> SaveEvents(IEnumerable<Event> events, DbTransaction? transaction)
    {
        IList<Event> savedEvents = new List<Event>();
        foreach (var @event in events)
        {
            if (@event.Id is null)
            {
                savedEvents.Add(await InsertEvent(@event, transaction));
            }
            else
            {
                savedEvents.Add(@event);
            }
        }
        return savedEvents;
    }

    private async Task<Event> InsertEvent(Event @event, DbTransaction? transaction)
    {
        var id = @event.Id ?? Guid.NewGuid();

        const string sql = """
                               INSERT INTO Events (Id, TripId, CreatedAt, Type, Payload)
                               VALUES (@Id, @TripId, @CreatedAt, @Type, @Payload);
                           """;

        await _connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            TripId = @event.TripId.ToString(),
            @event.CreatedAt,
            @event.Type,
            @event.Payload
        }, transaction: transaction);

        return @event with
        {
            Id = id
        };
    }

    private async Task<IEnumerable<Event>> FetchByTripId(Guid tripId)
    {
        const string sql = """
                               SELECT Id, TripId, CreatedAt, Type, Payload
                               FROM Events
                               WHERE TripId IS @TripId
                           """;
        var rows = await _connection.QueryAsync<EventRow>(sql, new { TripId = tripId.ToString() });
        return rows.Select(row => row.ToEvent());
    }

    private sealed record EventRow(
        string Id,
        string TripId,
        string CreatedAt,
        string Type,
        string Payload
    )
    {
        public Event ToEvent()
        {
            return new Event(
                Id: Guid.Parse(Id),
                TripId: Guid.Parse(TripId),
                CreatedAt: DateTime.Parse(CreatedAt),
                Type: Enum.Parse<EventType>(Type),
                Payload: Payload
            );
        }
    };

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
                status: Enum.Parse<TripStatus>(Status),
                []);
        }
    }
}
