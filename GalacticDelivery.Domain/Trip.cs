using System.Data.Common;

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
    public Guid RouteId { get; init; }
    public Guid DriverId { get; init; }
    public Guid VehicleId { get; init; }
    public TripStatus Status { get; private set; }
    public IList<Event> Events { get; } = new List<Event>();

    internal Trip(Guid? id, Guid routeId, Guid driverId, Guid vehicleId, TripStatus status)
    {
        Id = id;
        RouteId = routeId;
        DriverId = driverId;
        VehicleId = vehicleId;
        Status = status;
    }

    public static Trip Plan(Guid routeId, Guid driverId, Guid vehicleId)
    {
        return new Trip(null, routeId, driverId, vehicleId, TripStatus.Planned);
    }

    public void AddEvent(Event @event)
    {
        if (!IsLegalEvent(@event))
        {
            throw new InvalidOperationException($"Wrong event sequence: {@event.Type}");
        }
        Events.Add(@event);
        Status = EvalStatus(@event);
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

    private bool IsLegalEvent(Event @event)
    {
        switch (Status)
        {
            case TripStatus.Planned:
                return @event.Type == EventType.TripStarted;
            case TripStatus.InProgress:
                return @event.Type is EventType.TripCompleted or EventType.CheckpointPassed or EventType.Accident;
            case TripStatus.Finished:
                return false;
            default:
                throw new InvalidOperationException($"Unknown status: {Status}");
        }
    }
}

public interface ITripRepository
{
    public Task<Trip> Create(Trip trip, DbTransaction? transaction = null);
    public Task<Trip> Update(Trip trip, DbTransaction? transaction = null);
    public Task<Trip> Fetch(Guid tripId);
}