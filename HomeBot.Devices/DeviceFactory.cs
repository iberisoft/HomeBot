namespace HomeBot.Devices;

public class DeviceFactory : IAsyncDisposable
{
    readonly IMessageService m_MessageService;
    readonly DeviceInfo[] m_DeviceInfos;

    public DeviceFactory(IMessageService messageService, DeviceInfo[] deviceInfos)
    {
        m_MessageService = messageService;
        m_DeviceInfos = deviceInfos;

        messageService.OnMessageReceived += MessageService_OnMessageReceived;
    }

    readonly Dictionary<string, object> m_Devices = [];
    readonly List<IMessageDevice> m_MessageDevices = [];

    public async Task<object> CreateDevice(string name, bool subscribeMessages = false)
    {
        if (!m_Devices.TryGetValue(name, out var device))
        {
            var deviceInfo = m_DeviceInfos.FirstOrDefault(deviceInfo => deviceInfo.Name == name) ?? throw new ArgumentException($"Device '{name}' not found");
            var type = Type.GetType($"{GetType().Namespace}.{deviceInfo.Type}") ?? throw new ArgumentException($"Type '{deviceInfo.Type}' not found");
            device = CreateDevice(deviceInfo, type);
            if (subscribeMessages && device is IMessageDevice messageDevice)
            {
                await messageDevice.SubscribeMessages();
                m_MessageDevices.Add(messageDevice);
            }
            m_Devices.Add(name, device);
        }
        return device;
    }

    private object CreateDevice(DeviceInfo deviceInfo, Type type)
    {
        var device = Activator.CreateInstance(type, m_MessageService);
        foreach (var property in deviceInfo.Properties)
        {
            var propertyInfo = type.GetProperty(property.Key);
            propertyInfo?.SetValue(device, property.Value);
        }
        return device;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var device in m_MessageDevices)
        {
            await device.UnsubscribeMessages();
        }

        m_MessageService.OnMessageReceived -= MessageService_OnMessageReceived;
    }

    private async Task MessageService_OnMessageReceived(string topic, string payload)
    {
        foreach (var device in m_MessageDevices)
        {
            if (await device.ParseMessage(topic, payload))
            {
                break;
            }
        }
    }
}
