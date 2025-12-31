namespace HomeBot.Devices;

public class ShellyRelay(IMessageService messageService) : RelayBase(messageService)
{
    public string DeviceId { get; set; }

    protected override string GetStateTopic => $"shellies/shelly1-{DeviceId}/relay/0";

    protected override string SetStateTopic => $"shellies/shelly1-{DeviceId}/relay/0/command";

    protected override string StateToString(bool value) => value ? "on" : "off";

    protected override bool StateFromString(string value) => value == "on";
}
