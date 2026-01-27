using System.Data;
using System.Data.Common;
using GalacticDelivery.Common;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Application.Reports;

public record TripReportEventModel(
    Guid Id,
    Guid TripId,
    DateTime CreatedAt,
    string Type,
    string? Payload);

public record TripReportModel(
    Guid TripId,
    DateTime GeneratedAt,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    long? DurationSeconds,
    Guid DriverId,
    string DriverName,
    Guid VehicleId,
    string VehicleRegistrationNumber,
    Guid RouteId,
    string RouteOrigin,
    string RouteDestination,
    IReadOnlyList<string> CheckpointsPlanned,
    IReadOnlyList<string> CheckpointsPassed,
    long IncidentsCount,
    IReadOnlyList<TripReportEventModel> Events);

public interface ITripReportRepository
{
    Task<TripReportModel?> Fetch(Guid tripId);
    Task UpsertReport(TripReportModel report, DbTransaction? transaction = null);
    Task AddReportEvent(TripReportEventModel @event, DbTransaction? transaction = null);
}

public interface ITripReportProjection
{
    Task Apply(Event @event, DbTransaction? transaction = null, CancellationToken cancellationToken = default);
}

public sealed class TripReportProjection : ITripReportProjection
{
    private readonly ITripReportRepository _repo;
    private readonly ITripRepository _tripRepo;
    private readonly IRouteRepository _routeRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;

    public TripReportProjection(ITripReportRepository repo,
        ITripRepository tripRepo,
        IRouteRepository routeRepo,
        IDriverRepository driverRepo,
        IVehicleRepository vehicleRepo)
    {
        _repo = repo;
        _tripRepo = tripRepo;
        _routeRepo = routeRepo;
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
    }

    public async Task Apply(Event @event, DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var report = await _repo.Fetch(@event.TripId);
        if (report is null)
        {
            var trip = await _tripRepo.Fetch(@event.TripId, transaction);
            var route = await _routeRepo.Fetch(trip.RouteId, transaction);
            var driver = await _driverRepo.Fetch(trip.DriverId, transaction);
            var vehicle = await _vehicleRepo.Fetch(trip.VehicleId, transaction);

            var checkpointsPlanned = route.Checkpoints.Select(c => c.Name).ToList();

            report = new TripReportModel(
                TripId: trip.Id!.Value,
                GeneratedAt: @event.CreatedAt,
                CreatedAt: trip.CreatedAt,
                StartedAt: null,
                CompletedAt: null,
                DurationSeconds: null,
                DriverId: driver.Id!.Value,
                DriverName: $"{driver.FirstName} {driver.LastName}",
                VehicleId: vehicle.Id!.Value,
                VehicleRegistrationNumber: vehicle.RegNumber,
                RouteId: route.Id!.Value,
                RouteOrigin: route.Origin,
                RouteDestination: route.Destination,
                CheckpointsPlanned: checkpointsPlanned,
                CheckpointsPassed: new List<string>(),
                IncidentsCount: 0,
                Events: []);
        }

        var startedAt = report.StartedAt;
        var completedAt = report.CompletedAt;

        switch (@event.Type)
        {
            case EventType.TripStarted when startedAt is null:
                startedAt = @event.CreatedAt;
                break;
            case EventType.TripCompleted when completedAt is null:
                completedAt = @event.CreatedAt;
                break;
            case EventType.CheckpointPassed:
            case EventType.Accident:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event), "Unknown event type");
        }

        var checkpointsPassed = report.CheckpointsPassed.ToList();
        if (@event.Type == EventType.CheckpointPassed && !string.IsNullOrWhiteSpace(@event.Payload))
        {
            if (!checkpointsPassed.Contains(@event.Payload))
            {
                checkpointsPassed.Add(@event.Payload);
            }
        }

        var incidentsCount = report.IncidentsCount;
        if (@event.Type == EventType.Accident)
        {
            incidentsCount++;
        }

        var durationSeconds = report.DurationSeconds;
        if (startedAt is not null && completedAt is not null)
        {
            durationSeconds = (long)(completedAt.Value - startedAt.Value).TotalSeconds;
        }

        var reportEvent = new TripReportEventModel(
            Id: @event.Id ?? Guid.NewGuid(),
            TripId: @event.TripId,
            CreatedAt: @event.CreatedAt,
            Type: @event.Type.ToString(),
            Payload: @event.Payload);

        var events = report.Events.Concat([reportEvent]).ToList();

        var generatedAt = report.GeneratedAt;
        if (@event.CreatedAt > generatedAt)
        {
            generatedAt = @event.CreatedAt;
        }

        var updated = report with
        {
            GeneratedAt = generatedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            DurationSeconds = durationSeconds,
            CheckpointsPassed = checkpointsPassed,
            IncidentsCount = incidentsCount,
            Events = events
        };

        await _repo.AddReportEvent(reportEvent, transaction);
        await _repo.UpsertReport(updated, transaction);
    }
}

public sealed class GetTripReport(ITripReportRepository repo)
{
    public Task<TripReportModel?> Execute(Guid tripId) => repo.Fetch(tripId);
}
