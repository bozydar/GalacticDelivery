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
            routeId: Guid.NewGuid(),
            driverId: Guid.NewGuid(),
            carId: Guid.NewGuid(),
            status: status
        );
    }

    [Fact]
    public void Start_WhenStatusIsPlanned_ShouldChangeStatusToInProgress()
    {
        var trip = CreateTrip(TripStatus.Planned);

        trip.Start();

        Assert.Equal(TripStatus.InProgress, trip.Status);
    }

    [Theory]
    [InlineData(TripStatus.InProgress)]
    [InlineData(TripStatus.Finished)]
    public void Start_WhenStatusIsNotPlanned_ShouldThrowInvalidOperationException(
        TripStatus initialStatus)
    {
        var trip = CreateTrip(initialStatus);

        var exception = Assert.Throws<InvalidOperationException>(() => trip.Start());

        Assert.Equal("Trip hasn't been planned.", exception.Message);
    }

    [Fact]
    public void End_WhenStatusIsInProgress_ShouldChangeStatusToFinished()
    {
        var trip = CreateTrip(TripStatus.InProgress);

        trip.End();

        Assert.Equal(TripStatus.Finished, trip.Status);
    }

    [Theory]
    [InlineData(TripStatus.Planned)]
    [InlineData(TripStatus.Finished)]
    public void End_WhenStatusIsNotInProgress_ShouldThrowInvalidOperationException(
        TripStatus initialStatus)
    {
        var trip = CreateTrip(initialStatus);

        var exception = Assert.Throws<InvalidOperationException>(() => trip.End());

        Assert.Equal("Trip hasn't been progressed.", exception.Message);
    }
}