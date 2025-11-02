using ElevatorApp.Core;
using ElevatorApp.Web.Services;
using Microsoft.AspNetCore.SignalR;

namespace ElevatorApp.Web.Hubs;

public class ElevatorHub : Hub
{
    private readonly ElevatorControlSystem _controlSystem;
    private readonly SimulationService _simulationService;

    public ElevatorHub(ElevatorControlSystem controlSystem, SimulationService simulationService)
    {
        _controlSystem = controlSystem;
        _simulationService = simulationService;
    }

    public async Task RequestElevator(int originFloor, int destinationFloor)
    {
        if (originFloor < 1 || originFloor > 10 || destinationFloor < 1 || destinationFloor > 10 || originFloor == destinationFloor)
        {
            // basic validation, could return some localized message to the end users
            return;
        }

        _controlSystem.AddRequest(new PassengerRequest(originFloor, destinationFloor));
    }

    public void StartSimulation()
    {
        _simulationService.StartSimulation();
    }
}