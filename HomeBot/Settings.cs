using HomeBot.Devices;

namespace HomeBot;

class Settings
{
    public DeviceInfo[] Devices { get; set; } = [];

    public RelayRule[] RelayRules { get; set; } = [];

    public MqttBrokerSettings MqttBroker { get; set; }

    public TelegramSettings Telegram { get; set; }

    public class RelayRule
    {
        public string DeviceName { get; set; }

        public TimeOnly Time { get; set; }

        public bool State { get; set; }
    }

    public class MqttBrokerSettings
    {
        public string Host { get; set; }

        public int Port { get; set; }
    }

    public class TelegramSettings
    {
        public long ChatId { get; set; }

        public string BotToken { get; set; }

        public RelayButton[] RelayButtons { get; set; } = [];
    }

    public class RelayButton
    {
        public string DeviceName { get; set; }

        public string Text { get; set; }
    }
}
