namespace ElevatorApp.Core;

public class PassengerRequest
{
    public int OriginFloor { get; }
    public int DestinationFloor { get; }
    public Direction Direction => OriginFloor < DestinationFloor ? Direction.Up : Direction.Down;

    public PassengerRequest(int originFloor, int destinationFloor)
    {
        if (originFloor == destinationFloor)
            throw new ArgumentException("Origin and destination floors cannot be the same.");

        OriginFloor = originFloor;
        DestinationFloor = destinationFloor;
    }
}

public enum Direction
{
    Up,
    Down,
    Idle
}