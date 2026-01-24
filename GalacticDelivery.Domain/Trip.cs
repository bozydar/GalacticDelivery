using System.Data.Common;

namespace GalacticDelivery.Domain;

public enum TripStatus
{
    Planned,
    InProgress,
    Finished
}

public record Trip
{
    public Guid? Id { get; init; }
    public Guid RouteId { get; init; }
    public Guid DriverId { get; init; }
    public Guid VehicleId { get; init; }
    public TripStatus Status { get; private set; }

    internal Trip(Guid? id, Guid routeId, Guid driverId, Guid vehicleId, TripStatus status)
    {
        Id = id;
        RouteId = routeId;
        DriverId = driverId;
        VehicleId = vehicleId;
        Status = status;
    }
    
    public static Trip Plan(Guid routeId, Guid driverId, Guid vehicleId)
    {
        return new Trip(null, routeId, driverId, vehicleId, TripStatus.Planned);
    }

    public void Start()
    {
        if (Status != TripStatus.Planned)
        {
            throw new InvalidOperationException("Trip hasn't been planned.");
        }
        Status =  TripStatus.InProgress;
    }

    public void Finish()
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
    public Task<Trip> Create(Trip trip, DbTransaction? transaction = null);
    public Task<Trip> Update(Trip trip, DbTransaction? transaction = null);
    public Task<Trip> Fetch(Guid tripId);
}