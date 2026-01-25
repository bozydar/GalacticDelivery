namespace GalacticDelivery.Domain;

public record Checkpoint(
    string Name
);

public record Route(
    Guid? Id,
    string Origin,
    string Destination,
    IList<Checkpoint> Checkpoints
);

public interface IRouteRepository
{
    public Task<Route> Create(Route route);
    public Task<Route> Fetch(Guid routeId);
    public Task<IEnumerable<Route>> FetchAll();
}
