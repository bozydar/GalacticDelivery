using System.Data;
using System.Data.Common;

namespace GalacticDelivery.Domain;

public record Vehicle(Guid? Id, string RegNumber, Guid? CurrentTripId = null)
{
    public Vehicle AssignTrip(Guid tripId) => this with { CurrentTripId = tripId };
    public Vehicle UnassignTrip() => this with { CurrentTripId = null };
}

public interface IVehicleRepository
{
    public Task<Vehicle> Create(Vehicle vehicle);
    public Task<Vehicle> Update(Vehicle vehicle, DbTransaction? transaction = null);
    public Task<Vehicle> Fetch(Guid vehicleId, DbTransaction? transaction = null);
}
