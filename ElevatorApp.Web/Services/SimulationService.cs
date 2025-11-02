using ElevatorApp.Core;
using ElevatorApp.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ElevatorApp.Web.Services;

public class SimulationService : BackgroundService
{
    private readonly IHubContext<ElevatorHub> _hubContext;
    private readonly ElevatorControlSystem _controlSystem;
    private readonly object _sync = new();
    private bool _isSimulationRunning = false;

    public SimulationService(IHubContext<ElevatorHub> hubContext, ElevatorControlSystem controlSystem)
    {
        _hubContext = hubContext;
        _controlSystem = controlSystem;

        // Subscribe to state changes for each elevator to push updates via SignalR.
        foreach (var elevator in _controlSystem.Elevators)
        {
            elevator.StateChanged += BroadcastElevatorState;
        }

        _controlSystem.ElevatorAssigned += async (req, elevator) =>
        {
            await _hubContext.Clients.All.SendAsync("ElevatorAssigned", new
            {
                ElevatorId = elevator.Id,
                req.OriginFloor,
                req.DestinationFloor
            });
        };
    }

    public void StartSimulation()
    {
        lock (_sync)
        {
            _isSimulationRunning = true;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var running = false;

            lock (_sync)
            {
                running = _isSimulationRunning;
            }

            if (running)
            {
                // assign any pending requests to elevators
                _controlSystem.ProcessUnassignedRequests();

                foreach (var elevator in _controlSystem.Elevators)
                {
                    // Only start a new update if elevator is idle
                    if (!elevator.IsMoving)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await elevator.UpdateAsync();
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"Elevator {elevator.Id} update failed: {ex}");
                            }
                        }, stoppingToken);
                    }
                }
            }

            // The simulation "tick" rate.
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task BroadcastElevatorState(Elevator elevator)
    {
        await _hubContext.Clients.All.SendAsync("UpdateElevatorState", new
        {
            elevator.Id,
            elevator.CurrentFloor,
            Direction = elevator.CurrentDirection.ToString(),
            elevator.IsMoving,
            Requests = elevator.Requests.Select(r => $"Floor {r.OriginFloor} -> {r.DestinationFloor}").ToList()
        });
    }
}