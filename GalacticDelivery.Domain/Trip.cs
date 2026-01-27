using System.ComponentModel;
using System.Data;
using System.Data.Common;
using GalacticDelivery.Common;

namespace GalacticDelivery.Domain;

public enum TripStatus
{
    Planned,
    InProgress,
    Finished
}

public record Trip
{
    public Guid? Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid RouteId { get; init; }
    public Guid DriverId { get; init; }
    public Guid VehicleId { get; init; }
    public TripStatus Status { get; init; }
    public IList<Event> Events { get; init; }

    internal Trip(Guid? id, DateTime createdAt, Guid routeId, Guid driverId, Guid vehicleId, TripStatus status,
        IList<Event> events)
    {
        Id = id;
        RouteId = routeId;
        DriverId = driverId;
        VehicleId = vehicleId;
        Status = status;
        CreatedAt = createdAt;
        Events = events;
    }

    public static Trip Plan(Guid routeId, Guid driverId, Guid vehicleId)
    {
        return new Trip(null, DateTime.UtcNow, routeId, driverId, vehicleId, TripStatus.Planned, []);
    }

    public Result<Trip> AddEvent(Event @event)
    {
        var legality = IsLegalEvent(@event);
        if (legality.IsFailure)
        {
            return Result<Trip>.Failure(legality.Error!);
        }

        var events = Events.Concat([@event]);
        var status = EvalStatus(@event);
        return Result<Trip>.Success(this with { Events = events.ToList(), Status = status });
    }

    private TripStatus EvalStatus(Event @event)
    {
        var newStatus = @event.Type switch
        {
            EventType.TripStarted => TripStatus.InProgress,
            EventType.TripCompleted => TripStatus.Finished,
            _ => Status
        };
        return newStatus;
    }

    private Result IsLegalEvent(Event @event)
    {
        switch (Status)
        {
            case TripStatus.Planned:
                return @event.Type == EventType.TripStarted
                    ? Result.Success()
                    : Result.Failure(new Error("invalid_event",
                        $"Event {@event.Type} not allowed for {nameof(TripStatus.Planned)} trip."));
            case TripStatus.InProgress:
                return @event.Type is EventType.TripCompleted or EventType.CheckpointPassed or EventType.Accident
                    ? Result.Success()
                    : Result.Failure(new Error("invalid_event",
                        $"Event {@event.Type} not allowed for {nameof(TripStatus.InProgress)} trip."));
            case TripStatus.Finished:
                return Result.Failure(new Error("invalid_event",
                    $"Event {@event.Type} not allowed for {nameof(TripStatus.Finished)} trip."));
            default:
                throw new InvalidEnumArgumentException(nameof(@event), (int)Status, typeof(TripStatus));
        }
    }
}

public interface ITripRepository
{
    public Task<Trip> Create(Trip trip, DbTransaction? transaction = null);
    public Task<Trip> Update(Trip trip, DbTransaction? transaction = null);
    public Task<Trip> Fetch(Guid tripId, DbTransaction? transaction = null);
}