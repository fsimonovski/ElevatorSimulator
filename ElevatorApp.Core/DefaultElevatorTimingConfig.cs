namespace ElevatorApp.Core;

public class DefaultElevatorTimingConfig : IElevatorTimingConfig
{
    public int MoveTimeSeconds => 10;
    public int StopTimeSeconds => 10;
}
