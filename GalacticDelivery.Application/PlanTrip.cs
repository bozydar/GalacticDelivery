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

    public CreateTripUseCase(ITripRepository tripRepository)
    {
        _tripRepository = tripRepository;
    }

    public async Task<Guid> Execute(
        CreateTripCommand command,
        CancellationToken cancellationToken = default)
    {
        var trip = Trip.Plan(
            routeId: command.RouteId,
            driverId: command.DriverId,
            vehicleId: command.CarId
        );

        trip = await _tripRepository.Create(trip);

        return (Guid)trip.Id!;
    }
}