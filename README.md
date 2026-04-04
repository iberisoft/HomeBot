# HomeBot

[![.NET](https://github.com/iberisoft/HomeBot/actions/workflows/dotnet.yml/badge.svg)](https://github.com/iberisoft/HomeBot/actions/workflows/dotnet.yml)

Home automation project controlling relays via a Telegram bot.

## Configuration

Application settings are loaded from `appsettings.json`. Every option is described below, except `Serilog`, which is used for logging.

### `Devices`

An array of devices that the bot can control. Each entry has:

| Setting      | Description
|--------------|-------------
| `Name`       | Unique device name used in other sections.
| `Type`       | Device implementation class name in the `HomeBot.Devices` namespace.
| `Properties` | Key-value map applied to public settable properties on that type (e.g., `DeviceId`).

Currently, the following device types are implemented:
- `SonoffRelay`
- `ShellyRelay`

### `RelayRules`

Scheduled daily toggles. The scheduler checks about every 10 seconds and runs a rule when the local machine’s clock matches the rule’s time (hour and minute).
Each rule runs at most once per calendar day for that exact time.

| Setting      | Description
|--------------|-------------
| `DeviceName` | See `Name` in `Devices`.
| `Time`       | Time of day in `HH:mm` (24-hour).
| `State`      | `true` turns the relay on, `false` turns it off.

### `MqttBroker`

Settings for an MQTT broker.

| Setting | Description
|---------|-------------
| `Host`  | Broker hostname or IP (e.g., `localhost`).
| `Port`  | Broker port (e.g., `1883`).

### `Telegram`

Settings for the Telegram bot UI and notifications.

| Setting        | Description
|----------------|-------------
| `BotToken`     | Token from [@BotFather](https://t.me/BotFather).
| `ChatId`       | Numeric chat ID the bot listens to and sends messages to (group or user).
| `RelayButtons` | Inline keyboard.

Settings for the inline keyboard buttons.

| Setting      | Description
|--------------|-------------
| `DeviceName` | See `Name` in `Devices`.
| `Text`       | Label on the button (e.g., `Turn on light 💡`).
