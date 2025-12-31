using HomeBot.Devices;

namespace HomeBot;

class Settings
{
    public DeviceInfo[] Devices { get; set; } = [];

    public RelayRule[] RelayRules { get; set; } = [];

    public class RelayRule
    {
        public string DeviceName { get; set; }

        public TimeOnly Time { get; set; }

        public bool State { get; set; }
    }
}
