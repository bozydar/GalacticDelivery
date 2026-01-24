namespace GalacticDelivery.Domain;

public enum EventType
{
    TripStarted,
    TripCompleted,
    CheckpointPassed,
    CustomEvent
}

public class Event
{
    public Guid Id { get; init; }
    public DateTime TimeStamp { get; init; }
    public EventType EventType { get; init; }
    public string? Description { get; init; }
}