using System.Data;

namespace GalacticDelivery.Domain;

public enum EventType
{
    TripStarted,
    TripCompleted,
    CheckpointPassed,
    Accident
}

public record Event(
    Guid? Id,
    Guid TripId,
    DateTime CreatedAt,
    EventType Type,
    string? Payload);

public interface IEventRepository
{
    public Task<Event> Create(Event @event, IDbTransaction? transaction = null);
    public Task<IEnumerable<Event>> FetchByTripId(Guid tripId);
}