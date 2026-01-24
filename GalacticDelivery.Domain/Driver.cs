namespace GalacticDelivery.Domain;

public record Driver(Guid? Id, string FirstName, string LastName);

public interface IDriverRepository
{
    public Task<Driver> Create(Driver driver);
    public Task<Driver> Fetch(Guid driverId);
}