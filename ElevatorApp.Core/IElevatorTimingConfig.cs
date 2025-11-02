namespace ElevatorApp.Core;

public interface IElevatorTimingConfig
{
    int MoveTimeSeconds { get; }
    int StopTimeSeconds { get; }
}
