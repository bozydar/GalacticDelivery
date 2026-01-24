namespace GalacticDelivery.Domain;

public class Vehicle
{
    public Guid? Id { get; init; }
    public required string RegNumber  { get; init; }
}

public interface IVehicleRepository
{
    public Task<Vehicle> Create(Vehicle vehicle);
    public Task<Vehicle> Fetch(Guid vehicleId);
}