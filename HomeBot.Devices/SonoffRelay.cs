namespace HomeBot.Devices;

public class SonoffRelay(IMessageService messageService) : RelayBase(messageService)
{
    public string DeviceId { get; set; }

    protected override string GetStateTopic => $"stat/{DeviceId}/POWER";

    protected override string SetStateTopic => $"cmnd/{DeviceId}/POWER";

    protected override string StateToString(bool value) => value ? "ON" : "OFF";

    protected override bool StateFromString(string value) => value == "ON";
}
