using HomeBot.Devices;
using Microsoft.Extensions.Hosting;
using MQTTnet;

namespace HomeBot;

class MessageService : IMessageService, IHostedService
{
    readonly IMqttClient m_Client;

    public MessageService()
    {
        m_Client = new MqttClientFactory().CreateMqttClient();
        m_Client.ApplicationMessageReceivedAsync += e => OnMessageReceived?.Invoke(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = new MqttClientOptionsBuilder().WithTcpServer("localhost");
        var options = builder.Build();
        await m_Client.ConnectAsync(options, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var builder = new MqttClientDisconnectOptionsBuilder();
        var options = builder.Build();
        await m_Client.DisconnectAsync(options, cancellationToken);

        m_Client.Dispose();
    }

    public async Task SubscribeMessages(string topic) => await m_Client.SubscribeAsync(topic);

    public async Task UnsubscribeMessages(string topic) => await m_Client.UnsubscribeAsync(topic);

    public async Task PublishMessage(string topic, string payload) => await m_Client.PublishStringAsync(topic, payload);

    public event Func<string, string, Task> OnMessageReceived;
}
