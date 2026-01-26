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

    public ProcessEvent(ITripRepository tripRepository)
    {
        _tripRepository = tripRepository;
    }

    public async Task<Result<Guid>> Execute(
        ProcessEventCommand command)
    {
        var @event = ProcessEventCommandToEvent(command);
        
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
