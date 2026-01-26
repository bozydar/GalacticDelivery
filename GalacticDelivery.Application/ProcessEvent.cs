using System.Data.Common;
using GalacticDelivery.Common;
using GalacticDelivery.Domain;

namespace GalacticDelivery.Application;

public record ProcessEventCommand(
    Guid TripId,
    EventType Type,
    string Payload
);

public class ProcessEvent
{
    private readonly ITripRepository _tripRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IDriverRepository _driverRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public ProcessEvent(ITripRepository tripRepository, ITransactionManager transactionManager, IDriverRepository driverRepository, IVehicleRepository vehicleRepository)
    {
        _tripRepository = tripRepository;
        _transactionManager = transactionManager;
        _driverRepository = driverRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<Guid>> Execute(
        ProcessEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = ProcessEventCommandToEvent(command);
        return @event.Type switch
        {
            EventType.TripCompleted => await TripCompletedEvent(@event, cancellationToken),
            _ => await RegularEvent(@event)
        };
    }

    private async Task<Result<Guid>> RegularEvent(Event @event)
    {
        var trip = await _tripRepository.Fetch(@event.TripId);
        var addResult = trip.AddEvent(@event);
        if (addResult.IsFailure)
        {
            return Result<Guid>.Failure(addResult.Error!);
        }
        trip = addResult.Value!;
        trip = await _tripRepository.Update(trip);
        
        return Result<Guid>.Success((Guid)trip.Id!);
    }

    private async Task<Result<Guid>> TripCompletedEvent(
        Event @event, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            var trip = await _tripRepository.Fetch(@event.TripId);
            var addResult = trip.AddEvent(@event);
            if (addResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<Guid>.Failure(addResult.Error!);
            }
            
            await UnassignDriverAndVehicle(trip, transaction);

            trip = addResult.Value!;
            trip = await _tripRepository.Update(trip, transaction);

            await transaction.CommitAsync(cancellationToken);
            return Result<Guid>.Success((Guid)trip.Id!);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task UnassignDriverAndVehicle(Trip trip, DbTransaction transaction)
    {
        var driver = await _driverRepository.Fetch(trip.DriverId, transaction);
        var vehicle = await _vehicleRepository.Fetch(trip.VehicleId, transaction);

        driver = driver.UnassignTrip();
        vehicle = vehicle.UnassignTrip();
            
        _ = await _driverRepository.Update(driver, transaction);
        _ = await _vehicleRepository.Update(vehicle, transaction);
    }

    private Event ProcessEventCommandToEvent(ProcessEventCommand command)
    {
        return new Event(
            Id : null,
            TripId: command.TripId,
            Type: command.Type,
            CreatedAt: DateTime.UtcNow,
            Payload: command.Payload
        );
    }
}
