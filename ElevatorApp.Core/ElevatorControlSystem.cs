using System.Collections.Concurrent;

namespace ElevatorApp.Core;

public class ElevatorControlSystem
{
    public IReadOnlyList<Elevator> Elevators { get; }
    private readonly ConcurrentQueue<PassengerRequest> _unassignedRequests = new();

    private const int MoveSeconds = Elevator.MoveTimeSeconds;
    private const int StopSeconds = Elevator.StopTimeSeconds;
    private const int DirectionPenaltySeconds = 20; // penalty if elevator is going in the opposite direction

    public event Func<PassengerRequest, Elevator, Task>? ElevatorAssigned;

    public ElevatorControlSystem(int elevatorCount)
    {
        var elevators = new List<Elevator>();

        for (int i = 1; i <= elevatorCount; i++)
            elevators.Add(new Elevator(i));

        Elevators = elevators;
    }

    public void AddRequest(PassengerRequest request)
    {
        _unassignedRequests.Enqueue(request);
    }

    // Main logic to assign queued requests to the best elevator.
    public void ProcessUnassignedRequests()
    {
        while (_unassignedRequests.TryDequeue(out var req))
        {
            var bestElevator = FindBestElevatorForRequest(req);

            bestElevator.AddRequest(req);

            Console.WriteLine($"[{DateTime.UtcNow:O}] Assigned request {req.OriginFloor}->{req.DestinationFloor} to elevator {bestElevator.Id}");

            // fire and forget async event to avoid blocking
            _ = OnElevatorAssignedAsync(req, bestElevator);
        }
    }

    private async Task OnElevatorAssignedAsync(PassengerRequest request, Elevator elevator)
    {
        var handlers = ElevatorAssigned?.GetInvocationList();
        if (handlers == null) return;

        var tasks = handlers
            .OfType<Func<PassengerRequest, Elevator, Task>>()
            .Select(h =>
            {
                try { return h(request, elevator); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ElevatorAssigned handler threw: {ex}");
                    return Task.CompletedTask;
                }
            });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ElevatorAssigned async handler error: {ex}");
        }
    }

    private Elevator FindBestElevatorForRequest(PassengerRequest request)
    {
        Elevator? best = null;
        double bestETA = double.MaxValue;

        foreach (var e in Elevators)
        {
            // snapshot of assigned requests for approximate ETA
            var reqsSnapshot = e.Requests;

            // floors to travel to origin
            var floorsToOrigin = Math.Abs(e.CurrentFloor - request.OriginFloor);

            int expectedStops = reqsSnapshot.Count(r =>
            {
                if (e.CurrentDirection == Direction.Up)
                    return r.OriginFloor >= e.CurrentFloor && r.OriginFloor <= request.OriginFloor
                           || r.DestinationFloor >= e.CurrentFloor && r.DestinationFloor <= request.OriginFloor;
                else if (e.CurrentDirection == Direction.Down)
                    return r.OriginFloor <= e.CurrentFloor && r.OriginFloor >= request.OriginFloor
                           || r.DestinationFloor <= e.CurrentFloor && r.DestinationFloor >= request.OriginFloor;
                else
                    return false;
            });

            var directionPenalty = (e.CurrentDirection != Direction.Idle && e.CurrentDirection != request.Direction) ? DirectionPenaltySeconds : 0;

            var etaSeconds = floorsToOrigin * MoveSeconds + expectedStops * StopSeconds + directionPenalty;

            var tieBreaker = reqsSnapshot.Count;

            var score = etaSeconds + tieBreaker * 1.0;

            if (score < bestETA)
            {
                bestETA = score;
                best = e;
            }
        }

        return best ?? Elevators[0];
    }
}