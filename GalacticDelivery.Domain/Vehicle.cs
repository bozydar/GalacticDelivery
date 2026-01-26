using System.Data;

namespace GalacticDelivery.Domain;

public record Vehicle(Guid? Id, string RegNumber, Guid? CurrentTripId = null);

public interface IVehicleRepository
{
    public Task<Vehicle> Create(Vehicle vehicle);
    public Task<Vehicle> Update(Vehicle vehicle, IDbTransaction? transaction = null);
    public Task<Vehicle> Fetch(Guid vehicleId, IDbTransaction? transaction = null);
}
