namespace HomeBot.Devices;

public abstract class RelayBase(IMessageService messageService) : IRelay, IMessageDevice
{
    public async Task SubscribeMessages() => await messageService.SubscribeMessages(GetStateTopic);

    public async Task UnsubscribeMessages() => await messageService.UnsubscribeMessages(GetStateTopic);

    public async Task SetState(bool state) => await messageService.PublishMessage(SetStateTopic, StateToString(state));

    public async Task<bool> ParseMessage(string topic, string payload)
    {
        if (topic == GetStateTopic)
        {
            if (StateChanged != null)
            {
                await StateChanged.Invoke(StateFromString(payload));
            }
            return true;
        }
        return false;
    }

    public event Func<bool, Task> StateChanged;

    protected abstract string GetStateTopic { get; }

    protected abstract string SetStateTopic { get; }

    protected abstract string StateToString(bool value);

    protected abstract bool StateFromString(string value);
}
