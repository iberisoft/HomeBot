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
        m_Timer = new(TimeSpan.FromSeconds(10));
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

    DateTime m_Timestamp;
    readonly HashSet<Settings.RelayRule> m_RunRules = [];

    private async Task OnTimerTick()
    {
        if (SetTimestamp())
        {
            m_RunRules.Clear();
        }
        foreach (var rule in settings.RelayRules)
        {
            if (m_Timestamp.Hour == rule.Time.Hour && m_Timestamp.Minute == rule.Time.Minute && m_RunRules.Add(rule))
            {
                await RunRule(rule);
            }
        }
    }

    private bool SetTimestamp()
    {
        var newDay = false;
        var timestamp = DateTime.Now;
        if (m_Timestamp.Date != timestamp.Date)
        {
            newDay = true;
        }
        m_Timestamp = timestamp;
        return newDay;
    }

    private async Task RunRule(Settings.RelayRule rule)
    {
        if (await deviceFactory.CreateDevice(rule.DeviceName) is IRelay relay)
        {
            await relay.SetState(rule.State);
            Log.Information("Turn {State} relay {DeviceName} as per schedule", rule.State ? "ON" : "OFF", rule.DeviceName);
        }
    }
}
