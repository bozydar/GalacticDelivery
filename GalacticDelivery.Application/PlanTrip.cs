using GalacticDelivery.Domain;

namespace GalacticDelivery.Application;

public record CreateTripCommand(
    Guid RouteId,
    Guid DriverId,
    Guid CarId
);

public class CreateTripUseCase
{
    private readonly ITripRepository _tripRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ITransactionManager _transactionManager;

    public CreateTripUseCase(
        ITripRepository tripRepository,
        IDriverRepository driverRepository,
        IVehicleRepository vehicleRepository,
        ITransactionManager transactionManager)
    {
        _tripRepository = tripRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _transactionManager = transactionManager;
    }

    public async Task<Guid> Execute(
        CreateTripCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            var driver = await _driverRepository.Fetch(command.DriverId);
            if (driver.CurrentTripId is not null)
            {
                throw new InvalidOperationException($"Driver {command.DriverId} is already assigned to a trip.");
            }

            var vehicle = await _vehicleRepository.Fetch(command.CarId);
            if (vehicle.CurrentTripId is not null)
            {
                throw new InvalidOperationException($"Vehicle {command.CarId} is already assigned to a trip.");
            }

            var trip = Trip.Plan(
                routeId: command.RouteId,
                driverId: command.DriverId,
                vehicleId: command.CarId
            );

            trip = await _tripRepository.Create(trip, transaction);
            await _driverRepository.Update(
                driver with { CurrentTripId = trip.Id },
                transaction);
            await _vehicleRepository.Update(
                vehicle with { CurrentTripId = trip.Id },
                transaction);

            await transaction.CommitAsync(cancellationToken);
            return (Guid)trip.Id!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
