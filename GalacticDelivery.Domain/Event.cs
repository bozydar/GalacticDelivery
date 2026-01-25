namespace GalacticDelivery.Domain;

public enum EventType
{
    TripStarted,
    TripCompleted,
    CheckpointPassed,
    AccidentEvent
}

public class Event
{
    public Guid Id { get; init; }
    public Guid TripId { get; init; }
    public DateTime CreatedAt { get; init; }
    public EventType Type { get; init; }
    public string? Payload { get; init; }
}