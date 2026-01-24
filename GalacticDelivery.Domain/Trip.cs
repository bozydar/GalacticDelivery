using System.Data.Common;

namespace GalacticDelivery.Domain;

public enum TripStatus
{
    Planned,
    InProgress,
    Finished
}

public class Trip
{
    public Guid? Id { get; init; }
    public Guid RouteId { get; init; }
    public Guid DriverId { get; init; }
    public Guid CarId { get; init; }
    public TripStatus Status { get; private set; }

    public Trip(Guid? id, Guid routeId, Guid driverId, Guid carId, TripStatus status)
    {
        Id = id;
        RouteId = routeId;
        DriverId = driverId;
        CarId = carId;
        Status = status;
    }

    public void Start()
    {
        if (Status != TripStatus.Planned)
        {
            throw new InvalidOperationException("Trip hasn't been planned.");
        }
        Status =  TripStatus.InProgress;
    }

    public void End()
    {
        if (Status != TripStatus.InProgress) 
        {
            throw new InvalidOperationException("Trip hasn't been progressed.");
        }
        Status = TripStatus.Finished;
    }
}

public interface ITripRepository    
{
    public Task<Trip> Create(Trip driver, DbTransaction? transaction = null);
    public Task<Trip> Update(Trip driver, DbTransaction? transaction = null);
    public Task<Trip> Fetch(Guid tripId);
}