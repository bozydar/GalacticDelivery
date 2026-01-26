using System.Data;

namespace GalacticDelivery.Domain;

public record Driver(Guid? Id, string FirstName, string LastName, Guid? CurrentTripId = null);

public interface IDriverRepository
{
    public Task<Driver> Create(Driver driver, IDbTransaction? transaction = null);
    public Task<Driver> Update(Driver driver, IDbTransaction? transaction = null);
    public Task<Driver> Fetch(Guid driverId, IDbTransaction? transaction = null);
    public Task<IEnumerable<Guid>> FetchAllFree();
}