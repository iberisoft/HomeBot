namespace HomeBot.Devices;

public interface IRelay
{
    Task SetState(bool state);

    event Func<bool, Task> StateChanged;
}
