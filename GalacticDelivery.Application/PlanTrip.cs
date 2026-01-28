using System.Data;
using System.Data.Common;
using GalacticDelivery.Common;
using GalacticDelivery.Domain;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<PlanTrip> _logger;

    public PlanTrip(
        ITripRepository tripRepository,
        IDriverRepository driverRepository,
        IVehicleRepository vehicleRepository,
        ITransactionManager transactionManager,
        ILogger<PlanTrip> logger)
    {
        _tripRepository = tripRepository;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<Result<Guid>> Execute(
        CreateTripCommand command)
    {
        _logger.LogInformation("Planning trip RouteId={RouteId} DriverId={DriverId} VehicleId={VehicleId}",
            command.RouteId, command.DriverId, command.CarId);

        await using var transaction = await _transactionManager.BeginTransactionAsync();
        try
        {
            var driver = await _driverRepository.Fetch(command.DriverId);
            if (driver is null)
            {
                _logger.LogWarning("Driver not found. DriverId={DriverId}", command.DriverId);
                await transaction.RollbackAsync();
                return Result<Guid>.Failure(new Error(
                    "driver_not_found",
                    $"Driver {command.DriverId} not found."));
            }
            if (driver.CurrentTripId is not null)
            {
                _logger.LogWarning("Driver already assigned. DriverId={DriverId} TripId={TripId}",
                    command.DriverId, driver.CurrentTripId);
                await transaction.RollbackAsync();
                return Result<Guid>.Failure(new Error(
                    "driver_already_assigned",
                    $"Driver {command.DriverId} is already assigned to a trip."));
            }

            var vehicle = await _vehicleRepository.Fetch(command.CarId);
            if (vehicle is null)
            {
                _logger.LogWarning("Vehicle not found. VehicleId={VehicleId}", command.CarId);
                await transaction.RollbackAsync();
                return Result<Guid>.Failure(new Error(
                    "vehicle_not_found",
                    $"Vehicle {command.CarId} not found."));
            }
            if (vehicle.CurrentTripId is not null)
            {
                _logger.LogWarning("Vehicle already assigned. VehicleId={VehicleId} TripId={TripId}",
                    command.CarId, vehicle.CurrentTripId);
                await transaction.RollbackAsync();
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

            await transaction.CommitAsync();
            _logger.LogInformation(
                "Trip planned TripId={TripId} RouteId={RouteId} DriverId={DriverId} VehicleId={VehicleId}", trip.Id,
                trip.RouteId, trip.DriverId, trip.VehicleId);
            return Result<Guid>.Success((Guid)trip.Id!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to plan trip RouteId={RouteId} DriverId={DriverId} VehicleId={VehicleId}",
                command.RouteId, command.DriverId, command.CarId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task UpdateDriverAndVehicleTripIds(Driver driver, Vehicle vehicle, Trip trip,
        DbTransaction transaction)
    {
        _ = await _driverRepository.Update(
            driver with { CurrentTripId = trip.Id },
            transaction);
        _ = await _vehicleRepository.Update(
            vehicle with { CurrentTripId = trip.Id },
            transaction);
    }
}
