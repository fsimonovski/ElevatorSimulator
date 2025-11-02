using ElevatorApp.Core;
using Moq;

namespace ElevatorApp.Tests;

public class ElevatorControlSystemTests
{
    private readonly Mock<IElevatorTimingConfig> _mockConfig;

    public ElevatorControlSystemTests()
    {
        _mockConfig = new Mock<IElevatorTimingConfig>();
        _mockConfig.SetupGet(c => c.MoveTimeSeconds).Returns(0);
        _mockConfig.SetupGet(c => c.StopTimeSeconds).Returns(0);
    }

    [Fact]
    public void Constructor_ShouldInitializeGivenElevatorCount()
    {
        var ecs = new ElevatorControlSystem(3, _mockConfig.Object);

        Assert.Equal(3, ecs.Elevators.Count);
        Assert.All(ecs.Elevators, e => Assert.NotNull(e));
    }

    [Fact]
    public void AddRequest_ShouldAssignToElevator()
    {
        var ecs = new ElevatorControlSystem(2, _mockConfig.Object);

        ecs.AddRequest(new PassengerRequest(1, 5));
        ecs.ProcessUnassignedRequests();

        Assert.Contains(ecs.Elevators, e => e.Requests.Count == 1);
    }

    [Fact]
    public async Task ProcessUnassignedRequests_ShouldTriggerElevatorAssignedEvent()
    {
        var ecs = new ElevatorControlSystem(1, _mockConfig.Object);

        PassengerRequest? capturedReq = null;
        Elevator? capturedElev = null;

        ecs.ElevatorAssigned += (req, elev) =>
        {
            capturedReq = req;
            capturedElev = elev;
            return Task.CompletedTask;
        };

        var request = new PassengerRequest(2, 4);
        ecs.AddRequest(request);

        ecs.ProcessUnassignedRequests();
        await Task.Delay(10); // allow async event to complete

        Assert.Equal(request, capturedReq);
        Assert.NotNull(capturedElev);
        Assert.Equal(1, capturedElev!.Id);
    }

    [Fact]
    public void FindBestElevator_ShouldSelectClosestElevator()
    {
        var mockConfig = new Mock<IElevatorTimingConfig>();
        mockConfig.SetupGet(c => c.MoveTimeSeconds).Returns(1);
        mockConfig.SetupGet(c => c.StopTimeSeconds).Returns(0);

        var ecs = new ElevatorControlSystem(2, mockConfig.Object);

        ecs.Elevators[0].AddRequest(new PassengerRequest(1, 10));
        ecs.Elevators[1].AddRequest(new PassengerRequest(3, 4));

        typeof(Elevator).GetProperty(nameof(Elevator.CurrentFloor))!
            .SetValue(ecs.Elevators[1], 3);

        var request = new PassengerRequest(4, 8);
        ecs.AddRequest(request);
        ecs.ProcessUnassignedRequests();

        var assigned = ecs.Elevators.FirstOrDefault(e =>
            e.Requests.Any(r => r.OriginFloor == 4 && r.DestinationFloor == 8));

        Assert.NotNull(assigned);
        Assert.Equal(2, assigned!.Id);
    }

    [Fact]
    public void ProcessUnassignedRequests_ShouldDistributeAcrossElevators()
    {
        var ecs = new ElevatorControlSystem(3, _mockConfig.Object);

        ecs.AddRequest(new PassengerRequest(1, 5));
        ecs.AddRequest(new PassengerRequest(3, 7));
        ecs.AddRequest(new PassengerRequest(10, 1));

        ecs.ProcessUnassignedRequests();

        var totalAssigned = ecs.Elevators.Sum(e => e.Requests.Count);
        Assert.Equal(3, totalAssigned);
    }
}
