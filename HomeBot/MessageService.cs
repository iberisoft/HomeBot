using HomeBot.Devices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using Nito.AsyncEx;
using Serilog;

namespace HomeBot;

class MessageService : IMessageService, IHostedService
{
    readonly Settings.MqttBrokerSettings m_Settings;
    Task m_ConnectClientTask;
    CancellationTokenSource m_ConnectClientToken;
    readonly IMqttClient m_Client;

    public MessageService(IOptions<Settings> options)
    {
        m_Settings = options.Value.MqttBroker;

        m_Client = new MqttClientFactory().CreateMqttClient();
        m_Client.ApplicationMessageReceivedAsync += e => OnMessageReceived?.Invoke(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        m_ConnectClientTask = Task.Run(ConnectClient);
        return Task.CompletedTask;
    }

    private async Task ConnectClient()
    {
        using (m_ConnectClientToken = new())
        {
            while (true)
            {
                if (!m_Client.IsConnected)
                {
                    try
                    {
                        var builder = new MqttClientOptionsBuilder().WithTcpServer(m_Settings.Host, m_Settings.Port);
                        var options = builder.Build();
                        await m_Client.ConnectAsync(options);
                        await ResubscribeMessages();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Exception occurred");
                    }
                }
                try
                {
                    await Task.Delay(5000, m_ConnectClientToken.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task ResubscribeMessages()
    {
        using (await m_SubscribedTopicsLock.LockAsync())
        {
            foreach (var topic in m_SubscribedTopics)
            {
                await m_Client.SubscribeAsync(topic);
                Log.Information("Subscribed to messages with topic {Topic}", topic);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        m_ConnectClientToken.Cancel();
        await m_ConnectClientTask;

        var builder = new MqttClientDisconnectOptionsBuilder();
        var options = builder.Build();
        await m_Client.DisconnectAsync(options, cancellationToken);

        m_Client.Dispose();
    }

    readonly HashSet<string> m_SubscribedTopics = [];
    readonly AsyncLock m_SubscribedTopicsLock = new();

    public async Task SubscribeMessages(string topic)
    {
        if (m_Client.IsConnected)
        {
            await m_Client.SubscribeAsync(topic);
        }

        using (await m_SubscribedTopicsLock.LockAsync())
        {
            m_SubscribedTopics.Add(topic);
        }
    }

    public async Task UnsubscribeMessages(string topic)
    {
        if (m_Client.IsConnected)
        {
            await m_Client.UnsubscribeAsync(topic);
        }

        using (await m_SubscribedTopicsLock.LockAsync())
        {
            m_SubscribedTopics.Remove(topic);
        }
    }

    public async Task PublishMessage(string topic, string payload)
    {
        if (m_Client.IsConnected)
        {
            await m_Client.PublishStringAsync(topic, payload);
        }
    }

    public event Func<string, string, Task> OnMessageReceived;
}
