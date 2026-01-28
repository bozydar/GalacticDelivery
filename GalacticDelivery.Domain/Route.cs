using System.Data;
using System.Data.Common;

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
    public Task<Route?> Fetch(Guid routeId, DbTransaction? transaction = null);
    public Task<IEnumerable<Guid>> FetchAll();
}
