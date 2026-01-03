using HomeBot;
using HomeBot.Devices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

var host = CreateHostBuilder(args).Build();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(async () =>
{
    var deviceFactory = host.Services.GetRequiredService<DeviceFactory>();
    await deviceFactory.CloseAllDevices();
});

host.Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog((hostContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration))
        .ConfigureServices((hostingContext, services) =>
        {
            services.Configure<Settings>(hostingContext.Configuration);
            services.AddSingleton(serviceProvider =>
            {
                var messageService = serviceProvider.GetRequiredService<IMessageService>();
                var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;
                return new DeviceFactory(messageService, settings.Devices);
            });
            services.AddSingleton<IMessageService, MessageService>();
            services.AddHostedService(serviceProvider => (MessageService)serviceProvider.GetRequiredService<IMessageService>());
            services.AddHostedService<ScheduleService>();
            services.AddHostedService<TelegramService>();
        });
