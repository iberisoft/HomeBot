namespace HomeBot.Devices;

public class DeviceInfo
{
    public string Name { get; set; }

    public string Type { get; set; }

    public Dictionary<string, string> Properties { get; set; } = [];
}
