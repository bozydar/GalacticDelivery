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