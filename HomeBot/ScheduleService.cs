using HomeBot.Devices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace HomeBot;

class ScheduleService(DeviceFactory deviceFactory, IOptions<Settings> options) : IHostedService
{
    readonly Settings settings = options.Value;
    Task m_TimerTask;
    PeriodicTimer m_Timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        m_TimerTask = Task.Run(RunScheduler, CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task RunScheduler()
    {
        m_Timer = new(TimeSpan.FromSeconds(59));
        do
        {
            await OnTimerTick();
        }
        while (await m_Timer.WaitForNextTickAsync());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        m_Timer.Dispose();
        await m_TimerTask;
    }

    private async Task OnTimerTick()
    {
        var now = DateTime.Now;
        foreach (var rule in settings.RelayRules)
        {
            if (now.Hour == rule.Time.Hour && now.Minute == rule.Time.Minute && await deviceFactory.CreateDevice(rule.DeviceName) is IRelay relay)
            {
                await relay.SetState(rule.State);
                Log.Information("Turn {State} relay {DeviceName} as per schedule", rule.State ? "ON" : "OFF", rule.DeviceName);
            }
        }
    }
}
