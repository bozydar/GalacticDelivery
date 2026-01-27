using System.Data;
using System.Data.Common;
using GalacticDelivery.Common;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Application;

public record CreateTripCommand(
    Guid RouteId,
    Guid DriverId,
    Guid CarId
);

public class PlanTrip
{
    private readonly ITripRepository _tripRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ITransactionManager _transactionManager;

    public PlanTrip(
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

    public async Task<Result<Guid>> Execute(
        CreateTripCommand command,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            var driver = await _driverRepository.Fetch(command.DriverId);
            if (driver.CurrentTripId is not null)
            {
                transaction.Rollback();
                return Result<Guid>.Failure(new Error(
                    "driver_already_assigned",
                    $"Driver {command.DriverId} is already assigned to a trip."));
            }

            var vehicle = await _vehicleRepository.Fetch(command.CarId);
            if (vehicle.CurrentTripId is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure(new Error(
                    "vehicle_already_assigned",
                    $"Vehicle {command.CarId} is already assigned to a trip."));
            }

            var trip = Trip.Plan(
                routeId: command.RouteId,
                driverId: command.DriverId,
                vehicleId: command.CarId
            );

            trip = await _tripRepository.Create(trip, transaction);
            await UpdateDriverAndVehicleTripIds(driver, vehicle, trip, transaction);

            await transaction.CommitAsync(cancellationToken);
            return Result<Guid>.Success((Guid)trip.Id!);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task UpdateDriverAndVehicleTripIds(Driver driver, Vehicle vehicle, Trip trip, DbTransaction transaction)
    {
        _ = await _driverRepository.Update(
            driver with { CurrentTripId = trip.Id },
            transaction);
        _ = await _vehicleRepository.Update(
            vehicle with { CurrentTripId = trip.Id },
            transaction);
    }
}