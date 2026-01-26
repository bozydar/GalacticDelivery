using System.Data;
using System.Text.Json;
using Dapper;
using GalacticDelivery.Application.Reports;
using Microsoft.Data.Sqlite;

namespace GalacticDelivery.Infrastructure;

public sealed class SqliteTripReportRepository : ITripReportRepository
{
    private readonly SqliteConnection _connection;

    public SqliteTripReportRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    public async Task<TripReportModel?> Fetch(Guid tripId)
    {
        var row = await FetchTripReportRow(tripId);
        if (row is null)
        {
            return null;
        }

        var (checkpointsPlanned, checkpointsPassed, events) = await FetchTripReportEvents(tripId, row);
        return row.ToModel(checkpointsPlanned, checkpointsPassed, events);
    }

    private async Task<TripReportRow?> FetchTripReportRow(Guid tripId)
    {
        const string reportSql = """
                                     SELECT
                                       TripId,
                                       GeneratedAt,
                                       CreatedAt,
                                       StartedAt,
                                       CompletedAt,
                                       DurationSeconds,
                                       DriverId,
                                       DriverName,
                                       VehicleId,
                                       VehicleRegistrationNumber,
                                       RouteId,
                                       RouteOrigin,
                                       RouteDestination,
                                       CheckpointsPlanned,
                                       CheckpointsPassed,
                                       IncidentsCount
                                     FROM TripReports
                                     WHERE TripId = @TripId;
                                 """;

        var row = await _connection.QuerySingleOrDefaultAsync<TripReportRow>(
            reportSql,
            new { TripId = tripId.ToString() }
        );
        return row;
    }

    private async Task<(List<string> checkpointsPlanned, List<string> checkpointsPassed, List<TripReportEventModel> events)> FetchTripReportEvents(Guid tripId, TripReportRow row)
    {
        const string eventsSql = """
                                     SELECT
                                       Id,
                                       TripId,
                                       CreatedAt,
                                       Type,
                                       Payload
                                     FROM TripReportEvents
                                     WHERE TripId = @TripId
                                     ORDER BY CreatedAt ASC;
                                 """;

        var eventRows = await _connection.QueryAsync<TripReportEventRow>(
            eventsSql,
            new { TripId = tripId.ToString() }
        );

        var checkpointsPlanned = JsonSerializer.Deserialize<List<string>>(row.CheckpointsPlanned) ?? [];
        var checkpointsPassed = JsonSerializer.Deserialize<List<string>>(row.CheckpointsPassed) ?? [];
        var events = eventRows.Select(e => e.ToModel()).ToList();
        return (checkpointsPlanned, checkpointsPassed, events);
    }

    public Task UpsertReport(TripReportModel report, IDbTransaction? transaction = null)
    {
        const string sql = """
                               INSERT INTO TripReports (
                                 TripId,
                                 GeneratedAt,
                                 CreatedAt,
                                 StartedAt,
                                 CompletedAt,
                                 DurationSeconds,
                                 DriverId,
                                 DriverName,
                                 VehicleId,
                                 VehicleRegistrationNumber,
                                 RouteId,
                                 RouteOrigin,
                                 RouteDestination,
                                 CheckpointsPlanned,
                                 CheckpointsPassed,
                                 IncidentsCount
                               )
                               VALUES (
                                 @TripId,
                                 @GeneratedAt,
                                 @CreatedAt,
                                 @StartedAt,
                                 @CompletedAt,
                                 @DurationSeconds,
                                 @DriverId,
                                 @DriverName,
                                 @VehicleId,
                                 @VehicleRegistrationNumber,
                                 @RouteId,
                                 @RouteOrigin,
                                 @RouteDestination,
                                 @CheckpointsPlanned,
                                 @CheckpointsPassed,
                                 @IncidentsCount
                               )
                               ON CONFLICT(TripId) DO UPDATE SET
                                 GeneratedAt = excluded.GeneratedAt,
                                 CreatedAt = excluded.CreatedAt,
                                 StartedAt = excluded.StartedAt,
                                 CompletedAt = excluded.CompletedAt,
                                 DurationSeconds = excluded.DurationSeconds,
                                 DriverId = excluded.DriverId,
                                 DriverName = excluded.DriverName,
                                 VehicleId = excluded.VehicleId,
                                 VehicleRegistrationNumber = excluded.VehicleRegistrationNumber,
                                 RouteId = excluded.RouteId,
                                 RouteOrigin = excluded.RouteOrigin,
                                 RouteDestination = excluded.RouteDestination,
                                 CheckpointsPlanned = excluded.CheckpointsPlanned,
                                 CheckpointsPassed = excluded.CheckpointsPassed,
                                 IncidentsCount = excluded.IncidentsCount;
                           """;

        var parameters = new
        {
            TripId = report.TripId.ToString(),
            report.GeneratedAt,
            report.CreatedAt,
            report.StartedAt,
            report.CompletedAt,
            report.DurationSeconds,
            DriverId = report.DriverId.ToString(),
            report.DriverName,
            VehicleId = report.VehicleId.ToString(),
            report.VehicleRegistrationNumber,
            RouteId = report.RouteId.ToString(),
            report.RouteOrigin,
            report.RouteDestination,
            CheckpointsPlanned = JsonSerializer.Serialize(report.CheckpointsPlanned),
            CheckpointsPassed = JsonSerializer.Serialize(report.CheckpointsPassed),
            report.IncidentsCount
        };

        return _connection.ExecuteAsync(sql, parameters, transaction: transaction);
    }

    public Task AddReportEvent(TripReportEventModel @event, IDbTransaction? transaction = null)
    {
        const string sql = """
                               INSERT INTO TripReportEvents (Id, TripId, CreatedAt, Type, Payload)
                               VALUES (@Id, @TripId, @CreatedAt, @Type, @Payload);
                           """;

        var parameters = new
        {
            Id = @event.Id.ToString(),
            TripId = @event.TripId.ToString(),
            @event.CreatedAt,
            @event.Type,
            @event.Payload
        };

        return _connection.ExecuteAsync(sql, parameters, transaction: transaction);
    }

    private sealed record TripReportRow(
        string TripId,
        string GeneratedAt,
        string CreatedAt,
        string? StartedAt,
        string? CompletedAt,
        long? DurationSeconds,
        string DriverId,
        string DriverName,
        string VehicleId,
        string VehicleRegistrationNumber,
        string RouteId,
        string RouteOrigin,
        string RouteDestination,
        string CheckpointsPlanned,
        string CheckpointsPassed,
        long IncidentsCount)
    {
        public TripReportModel ToModel(IReadOnlyList<string> checkpointsPlanned,
            IReadOnlyList<string> checkpointsPassed, IReadOnlyList<TripReportEventModel> events)
        {
            return new TripReportModel(
                TripId: Guid.Parse(TripId),
                GeneratedAt: DateTime.Parse(GeneratedAt),
                CreatedAt: DateTime.Parse(CreatedAt),
                StartedAt: StartedAt is null ? null : DateTime.Parse(StartedAt),
                CompletedAt: CompletedAt is null ? null : DateTime.Parse(CompletedAt),
                DurationSeconds: DurationSeconds is null ? null : (int)DurationSeconds,
                DriverId: Guid.Parse(DriverId),
                DriverName: DriverName,
                VehicleId: Guid.Parse(VehicleId),
                VehicleRegistrationNumber: VehicleRegistrationNumber,
                RouteId: Guid.Parse(RouteId),
                RouteOrigin: RouteOrigin,
                RouteDestination: RouteDestination,
                CheckpointsPlanned: checkpointsPlanned,
                CheckpointsPassed: checkpointsPassed,
                IncidentsCount: IncidentsCount,
                Events: events);
        }
    }

    private sealed record TripReportEventRow(
        string Id,
        string TripId,
        string CreatedAt,
        string Type,
        string? Payload)
    {
        public TripReportEventModel ToModel()
        {
            return new TripReportEventModel(
                Id: Guid.Parse(Id),
                TripId: Guid.Parse(TripId),
                CreatedAt: DateTime.Parse(CreatedAt),
                Type: Type,
                Payload: Payload);
        }
    }
}
