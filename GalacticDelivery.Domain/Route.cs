namespace GalacticDelivery.Domain;

public class Route
{
    public Guid? Id { get; init; }
    public required string StartPoint { get; init; }
    public required string EndPoint { get; init; }
}

public interface IRouteRepository
{
    public Task<Route> Create(Route route);
    public Task<Route> Fetch(Guid routeId);
}
