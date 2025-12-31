
namespace HomeBot.Devices;

public interface IMessageDevice
{
    Task SubscribeMessages();

    Task UnsubscribeMessages();

    Task<bool> ParseMessage(string topic, string payload);
}
