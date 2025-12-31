namespace HomeBot.Devices;

public interface IMessageService
{
    public Task SubscribeMessages(string topic);

    public Task UnsubscribeMessages(string topic);

    public Task PublishMessage(string topic, string payload);

    event Func<string, string, Task> OnMessageReceived;
}
