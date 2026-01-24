namespace GalacticDelivery.Domain;

public record Vehicle(Guid? Id, string RegNumber);

public interface IVehicleRepository
{
    public Task<Vehicle> Create(Vehicle vehicle);
    public Task<Vehicle> Fetch(Guid vehicleId);
}