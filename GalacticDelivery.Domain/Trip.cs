namespace GalacticDelivery.Domain;

public class Trip
{
    public Guid? Id { get; init; }
    public Guid RouteId { get; init; }
    public Guid DriverId { get; init; }
    public Guid CarId { get; init; }
}

public interface ITripRepository
{
    public Task<Trip> Create(Trip driver);
    public Task<Trip> Fetch(Guid tripId);
    public Task Start(Guid tripId);
    public Task End(Guid tripId);
    public Task Event(Guid tripId, Event @event);
}