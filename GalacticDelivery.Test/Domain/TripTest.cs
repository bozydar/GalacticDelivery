using GalacticDelivery.Domain;

namespace GalacticDelivery.Test.Domain;

using System;
using Xunit;

public class TripTests
{
    private static Trip CreateTrip(TripStatus status)
    {
        return new Trip(
            id: Guid.NewGuid(),
            createdAt: new DateTime(),
            routeId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            vehicleId: Guid.NewGuid(),
            status: status,
            events: []
        );
    }

    private static Event CreateEvent(Guid tripId, EventType type, string? payload = null)
    {
        return new Event(
            Id: Guid.NewGuid(),
            TripId: tripId,
            CreatedAt: DateTime.UtcNow,
            Type: type,
            Payload: payload);
    }

    [Fact]
    public void Plan_ShouldInitializePlannedStatusAndEmptyEvents()
    {
        var trip = Trip.Plan(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(TripStatus.Planned, trip.Status);
        Assert.Empty(trip.Events);
    }

    [Fact]
    public void AddEvent_WhenTripStarted_ShouldSetStatusToInProgress()
    {
        var trip = CreateTrip(TripStatus.Planned);

        trip = trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripStarted));

        Assert.Equal(TripStatus.InProgress, trip.Status);
    }

    [Fact]
    public void AddEvent_WhenTripCompleted_ShouldSetStatusToFinished()
    {
        var trip = CreateTrip(TripStatus.InProgress);

        trip = trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripCompleted));

        Assert.Equal(TripStatus.Finished, trip.Status);
    }

    [Fact]
    public void AddEvent_WhenNonStatusEvent_ShouldNotChangeStatus()
    {
        var trip = CreateTrip(TripStatus.InProgress);

        trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.CheckpointPassed, "Checkpoint-1"));

        Assert.Equal(TripStatus.InProgress, trip.Status);
    }

    [Fact]
    public void AddEvent_WhenStartedThenCompleted_ShouldEndAsFinished()
    {
        var trip = CreateTrip(TripStatus.Planned);

        trip = trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripStarted));
        trip = trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripCompleted));

        Assert.Equal(TripStatus.Finished, trip.Status);
    }

    [Fact]
    public void AddEvent_WhenPlannedThenNotStarted_ShouldThrowInvalidOperationException()
    {
        var trip = CreateTrip(TripStatus.Planned);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripCompleted)));

        Assert.Equal($"Wrong event sequence: TripCompleted", exception.Message);
    }

    [Fact]
    public void AddEvent_WhenTripIsFinished_ShouldRejectAnotherCompletion()
    {
        var trip = CreateTrip(TripStatus.InProgress);

        trip = trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripCompleted));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripCompleted)));

        Assert.Equal("Wrong event sequence: TripCompleted", exception.Message);
    }

    [Fact]
    public void AddEvent_WhenTripIsInProgress_ShouldRejectTripStarted()
    {
        var trip = CreateTrip(TripStatus.InProgress);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripStarted)));

        Assert.Equal("Wrong event sequence: TripStarted", exception.Message);
    }

    [Fact]
    public void AddEvent_WhenTripIsFinished_ShouldRejectTripStarted()
    {
        var trip = CreateTrip(TripStatus.Finished);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            trip.AddEvent(CreateEvent(trip.Id!.Value, EventType.TripStarted)));

        Assert.Equal("Wrong event sequence: TripStarted", exception.Message);
    }
}