using ElevatorApp.Core;
using Moq;

namespace ElevatorApp.Tests;

public class ElevatorTests
{
    private readonly Mock<IElevatorTimingConfig> _mockConfig;

    public ElevatorTests()
    {
        _mockConfig = new Mock<IElevatorTimingConfig>();
        _mockConfig.SetupGet(c => c.MoveTimeSeconds).Returns(0);
        _mockConfig.SetupGet(c => c.StopTimeSeconds).Returns(0);
    }

    [Fact]
    public void AddRequest_ShouldEnqueueRequest_AndTriggerStateChange()
    {
        // Arrange
        var elevator = new Elevator(1, _mockConfig.Object);
        bool eventTriggered = false;
        elevator.StateChanged += e =>
        {
            eventTriggered = true;
            return Task.CompletedTask;
        };

        var request = new PassengerRequest(1, 5);

        // Act
        elevator.AddRequest(request);

        // Assert
        Assert.Single(elevator.Requests);
        Assert.Equal(request, elevator.Requests.First());
        Assert.True(eventTriggered);
    }

    [Fact]
    public async Task UpdateAsync_ShouldMoveElevatorUp_WhenRequestIsAbove()
    {
        // Arrange
        var elevator = new Elevator(1, _mockConfig.Object);
        elevator.AddRequest(new PassengerRequest(1, 3));

        // Act
        await elevator.UpdateAsync(); // should move to floor 2
        await elevator.UpdateAsync(); // should move to floor 3

        // Assert
        Assert.Equal(3, elevator.CurrentFloor);
        Assert.False(elevator.IsMoving);
        Assert.Equal(Direction.Up, elevator.CurrentDirection);
    }

    [Fact]
    public async Task UpdateAsync_ShouldBecomeIdle_AfterDropOff()
    {
        // Arrange
        var elevator = new Elevator(1, _mockConfig.Object);
        elevator.AddRequest(new PassengerRequest(1, 2));

        // Act
        await elevator.UpdateAsync(); // move to 2
        await elevator.UpdateAsync(); // drop off

        // Assert
        Assert.Empty(elevator.Requests);
        Assert.Equal(Direction.Idle, elevator.CurrentDirection);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotProcessConcurrently()
    {
        // Arrange
        var elevator = new Elevator(1, _mockConfig.Object);
        elevator.AddRequest(new PassengerRequest(1, 3));

        // Act
        var task1 = elevator.UpdateAsync();
        var task2 = elevator.UpdateAsync();
        await Task.WhenAll(task1, task2);

        // Assert
        Assert.False(elevator.IsMoving);
        Assert.InRange(elevator.CurrentFloor, 1, 3);
    }

    [Fact]
    public void DetermineNextDirection_ShouldSetDirectionBasedOnRequest()
    {
        // Arrange
        var elevator = new Elevator(1, _mockConfig.Object);

        // Move elevator manually to floor 5
        typeof(Elevator).GetProperty(nameof(Elevator.CurrentFloor))!
            .SetValue(elevator, 5);

        elevator.AddRequest(new PassengerRequest(5, 8)); // should go up

        // Act (trigger direction determination indirectly)
        var updateMethod = typeof(Elevator)
            .GetMethod("DetermineNextDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        updateMethod.Invoke(elevator, null);

        // Assert
        Assert.Equal(Direction.Up, elevator.CurrentDirection);
    }
}
