using System.Data;
using Dapper;
using GalacticDelivery.Domain;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteEventRepository : IEventRepository
{
    private readonly SqliteConnection _connection;

    public SqliteEventRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<Event> Create(Event @event, IDbTransaction? transaction)
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

    public async Task<IEnumerable<Event>> FetchByTripId(Guid tripId)
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
}