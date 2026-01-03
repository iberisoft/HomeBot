using HomeBot.Devices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HomeBot;

class TelegramService(DeviceFactory deviceFactory, IOptions<Settings> options) : IHostedService
{
    readonly Settings.TelegramSettings settings = options.Value.Telegram;
    ITelegramBotClient m_BotClient;
    CancellationTokenSource m_BotClientToken;
    readonly Dictionary<string, IRelay> m_Relays = [];
    readonly Dictionary<string, bool> m_RelayStates = [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (settings.BotToken == "")
        {
            Log.Warning("Telegram BotToken setting not defined");
            return;
        }

        m_BotClient = new TelegramBotClient(settings.BotToken);
        m_BotClientToken = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery]
        };
        m_BotClient.StartReceiving((_, update, _) => HandleUpdate(update), (_, exception, _) => HandleError(exception), receiverOptions, m_BotClientToken.Token);
        await CreateRelays();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (m_BotClient == null)
        {
            return;
        }

        await DeleteRelayMessage();

        m_BotClientToken.Cancel();
        m_BotClientToken.Dispose();
    }

    private async Task CreateRelays()
    {
        foreach (var button in settings.RelayButtons)
        {
            var device = await deviceFactory.CreateDevice(button.DeviceName, true);
            if (device is IRelay relay)
            {
                m_Relays[button.DeviceName] = relay;
                relay.StateChanged += async state =>
                {
                    if (!m_RelayStates.TryGetValue(button.DeviceName, out var oldState) || oldState != state)
                    {
                        m_RelayStates[button.DeviceName] = state;
                        await UpdateRelayMessage();
                    }
                };
            }
        }
    }

    private async Task HandleUpdate(Update update)
    {
        switch (update.Type)
        {
            case UpdateType.Message:
                await HandleMessage(update);
                break;
            case UpdateType.CallbackQuery:
                await HandleCallbackQuery(update);
                break;
        }
    }

    private async Task HandleMessage(Update update)
    {
        if (settings.ChatId == 0)
        {
            await m_BotClient.SendMessage(update.Message.Chat.Id, $"Assign `{update.Message.Chat.Id}` to `ChatId` setting then restart service", parseMode: ParseMode.MarkdownV2);
            return;
        }

        if (settings.ChatId == update.Message.Chat.Id)
        {
            var command = update.Message.Text.Split()[0].Split('@')[0];
            switch (command)
            {
                case "/update":
                    await HandleUpdateCommand();
                    break;
                case "/schedule":
                    await HandleScheduleCommand();
                    break;
            }
        }
    }

    private async Task HandleUpdateCommand()
    {
        await DeleteRelayMessage();
        await UpdateRelayMessage();
    }

    private async Task HandleScheduleCommand()
    {
        await m_BotClient.SendMessage(settings.ChatId, BuildScheduleTexts(), parseMode: ParseMode.MarkdownV2);
    }

    private async Task HandleCallbackQuery(Update update)
    {
        if (settings.ChatId == update.CallbackQuery.Message.Chat.Id)
        {
            await HandleButtonClick(update.CallbackQuery.Data);
        }
        await m_BotClient.AnswerCallbackQuery(update.CallbackQuery.Id);
    }

    private async Task HandleButtonClick(string relayName)
    {
        var newState = !m_RelayStates[relayName];
        await m_Relays[relayName].SetState(newState);
        Log.Information("Toggle {State} relay {DeviceName}", newState ? "ON" : "OFF", relayName);
    }

    private static Task HandleError(Exception exception)
    {
        Log.Error(exception, "Exception occurred");
        return Task.CompletedTask;
    }

    Message m_RelayMessage;

    private async Task UpdateRelayMessage()
    {
        if (settings.ChatId == 0)
        {
            Log.Warning("Telegram ChatId setting not defined");
            return;
        }

        if (m_RelayMessage == null)
        {
            m_RelayMessage = await m_BotClient.SendMessage(settings.ChatId, BuildRelayTexts(), parseMode: ParseMode.MarkdownV2, replyMarkup: BuildRelayKeyboard());
        }
        else
        {
            await m_BotClient.EditMessageText(settings.ChatId, m_RelayMessage.Id, BuildRelayTexts(), parseMode: ParseMode.MarkdownV2, replyMarkup: BuildRelayKeyboard());
        }
    }

    private async Task DeleteRelayMessage()
    {
        if (m_RelayMessage != null)
        {
            await m_BotClient.DeleteMessage(settings.ChatId, m_RelayMessage.Id);
            m_RelayMessage = null;
        }
    }

    private string BuildRelayTexts()
    {
        var lines = settings.RelayButtons.Select(button => m_RelayStates[button.DeviceName] ? $"🟢 *{button.Text}*" : $"🔴 {button.Text}");
        return string.Join("\n\n", lines);
    }

    private InlineKeyboardMarkup BuildRelayKeyboard()
    {
        var buttons = settings.RelayButtons.Select(button => InlineKeyboardButton.WithCallbackData(button.Text, button.DeviceName));
        return new(buttons);
    }

    private string BuildScheduleTexts()
    {
        var lines = options.Value.RelayRules
            .GroupBy(rule => rule.DeviceName)
            .Select(group =>
            {
                var deviceName = group.Key;
                var deviceText = settings.RelayButtons.FirstOrDefault(button => button.DeviceName == deviceName)?.Text ?? deviceName;
                var lines = group.Select(rule => $"• `{rule.Time:HH:mm}` → `{(rule.State ? "ON" : "OFF")}`");
                return string.Join("\n", [deviceText, ..lines]);
            });
        return string.Join("\n\n", lines);
    }
}
