namespace HomeBot.Devices;

public class SonoffRelay(IMessageService messageService) : RelayBase(messageService)
{
    protected override string GetStateTopic => "stat/sonoff/POWER";

    protected override string SetStateTopic => "cmnd/sonoff/POWER";

    protected override string StateToString(bool value) => value ? "ON" : "OFF";

    protected override bool StateFromString(string value) => value == "ON";
}
