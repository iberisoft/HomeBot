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
| `RelayButtons` | Inline keyboard buttons.

Settings for inline keyboard buttons.

| Setting      | Description
|--------------|-------------
| `DeviceName` | See `Name` in `Devices`.
| `Text`       | Label on the button (e.g., `Turn on light 💡`).

## Connecting to Telegram

Use the `Telegram` settings to connect the bot to Telegram.

Initially, `BotToken` is empty, the service logs a warning `Telegram BotToken setting not defined` and does not start the Telegram client.

1. Open [@BotFather](https://t.me/BotFather) in Telegram, create a new bot (`/newbot`), and copy the **token** it gives you.
2. Set `BotToken` to that token.
3. Restart the service.

After the token is set, the bot can receive messages. `ChatId` is still `0`, the service logs a warning `Telegram ChatId setting not defined`.

1. Add the bot to a group where you want to control relays, or open a private chat with the bot.
2. Send the bot any text message from that group or private chat.
3. The bot replies with a message that tells you which number to put in `ChatId`.
4. Set `ChatId` to that number.
5. Restart the service.

Only that chat may use commands and inline buttons; messages from other chats are ignored once `ChatId` is configured.

## Bot commands

The service handles slash commands in the configured chat.

| Command     | What it does
|-------------|---------------
| `/update`   | Updates the current control message with the inline keyboard.
| `/schedule` | Sends a message listing `RelayRules` for each device and time.

You should register these commands in [@BotFather](https://t.me/BotFather) via `/setcommands` to show them in the Telegram UI.
The bot will respond to these commands even if you skip registration.

# Mosquitto Broker

Use this when the MQTT broker runs on a Raspberry Pi and HomeBot (or other clients) connect over the network with **no username/password**.

## Installation

```bash
sudo apt update
sudo apt install -y mosquitto
```

## Configuration

Mosquitto expects an explicit listener. Create a file:

```bash
sudo nano /etc/mosquitto/conf.d/local.conf
```

Minimal content for MQTT on port `1883` from any interface, with anonymous clients allowed:

```
listener 1883
allow_anonymous true
```

Save the file, then restart Mosquitto:

```bash
sudo systemctl restart mosquitto
sudo systemctl enable mosquitto
```
