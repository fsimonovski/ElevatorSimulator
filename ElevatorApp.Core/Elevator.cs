using System.Collections.Concurrent;

namespace ElevatorApp.Core;

public class Elevator
{
    public const int MoveTimeSeconds = 10;
    public const int StopTimeSeconds = 10;

    public int Id { get; }
    public int CurrentFloor { get; private set; } = 1;
    public Direction CurrentDirection { get; private set; } = Direction.Idle;

    private int _isMoving = 0;
    public bool IsMoving => Volatile.Read(ref _isMoving) == 1;

    private readonly ConcurrentQueue<PassengerRequest> _requests = new();
    public IReadOnlyList<PassengerRequest> Requests => _requests.ToArray();

    public event Func<Elevator, Task>? StateChanged;

    public Elevator(int id)
    {
        Id = id;
    }

    public void AddRequest(PassengerRequest request)
    {
        _requests.Enqueue(request);

        OnStateChanged();
    }

    // The main logic loop for an elevator on each "tick" of the simulation.
    public async Task UpdateAsync()
    {
        // if another tick has already set the _isMoving flag, skip the update to avoid concurrent moves
        if (Interlocked.CompareExchange(ref _isMoving, 1, 0) == 1)
            return;

        try
        {
            // handle stops (drop-offs and pickups) at the current floor
            var hadStop = ProcessCurrentFloorActions();
            if (hadStop)
            {
                OnStateChanged();

                await Task.Delay(TimeSpan.FromSeconds(StopTimeSeconds));
            }

            // determine next destination based on current requests and direction
            DetermineNextDirection();

            // move to the next floor if needed.
            if (CurrentDirection != Direction.Idle)
            {
                OnStateChanged();

                await Task.Delay(TimeSpan.FromSeconds(MoveTimeSeconds));

                CurrentFloor += CurrentDirection == Direction.Up ? 1 : -1;

                OnStateChanged();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isMoving, 0);
        }
    }

    // handles drop-offs and new pickups at the current floor
    private bool ProcessCurrentFloorActions()
    {
        bool hadAction = false;

        // Drop off passengers whose destination is the current floor.
        var temp = new List<PassengerRequest>();
        while (_requests.TryDequeue(out var req))
        {
            if (req.DestinationFloor == CurrentFloor)
            {
                // drop off the passengers
                hadAction = true;
                continue;
            }

            if (req.OriginFloor == CurrentFloor)
            {
                // pickup occurs and we count it as a stopping, the passengers will later be dropped off at the destination
                hadAction = true;
                temp.Add(req);
                continue;
            }

            temp.Add(req);
        }

        // re enque remaining requests in same order as when we started reading
        foreach (var req in temp)
            _requests.Enqueue(req);

        // if we are now idle, we can pick up passengers going in either direction
        if (_requests.IsEmpty)
            CurrentDirection = Direction.Idle;

        return hadAction;
    }

    private void DetermineNextDirection()
    {
        var snapshot = _requests.ToArray();

        if (snapshot.Length == 0)
        {
            CurrentDirection = Direction.Idle;
            return;
        }

        // if idle, pick direction of the first request
        if (CurrentDirection == Direction.Idle)
        {
            var next = snapshot[0];

            if (next.OriginFloor > CurrentFloor)
                CurrentDirection = Direction.Up;
            else if (next.OriginFloor < CurrentFloor)
                CurrentDirection = Direction.Down;
            else // pickup at current floor
                CurrentDirection = next.Direction;
        }

        // continue in the same direction if there are more requests in that direction.
        bool hasMoreInDirection = CurrentDirection == Direction.Up
            ? snapshot.Any(r => r.OriginFloor > CurrentFloor || r.DestinationFloor > CurrentFloor)
            : snapshot.Any(r => r.OriginFloor < CurrentFloor || r.DestinationFloor < CurrentFloor);

        if (!hasMoreInDirection)
        {
            // If no more requests in the current direction, switch directions or go idle.
            CurrentDirection = snapshot.Any() ? (CurrentDirection == Direction.Up ? Direction.Down : Direction.Up) : Direction.Idle;
        }
    }

    protected virtual void OnStateChanged()
    {
        var handler = StateChanged;

        handler?.Invoke(this);
    }
}