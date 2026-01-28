using System.Data;
using System.Data.Common;

namespace GalacticDelivery.Domain;

public record Driver(Guid? Id, string FirstName, string LastName, Guid? CurrentTripId = null)
{
    public Driver AssignTrip(Guid tripId) => this with { CurrentTripId = tripId };
    public Driver UnassignTrip() => this with { CurrentTripId = null };
}

public interface IDriverRepository
{
    public Task<Driver> Update(Driver driver, DbTransaction? transaction = null);
    public Task<Driver?> Fetch(Guid driverId, DbTransaction? transaction = null);
    public Task<IEnumerable<Guid>> FetchAllFree();
}
