using System;
using System.Text;
using Discord;
using Newtonsoft.Json;

namespace QuaverBot;

public struct BotConfig
{
    public string Token;

    [JsonConverter(typeof(LogSeverityConverter))]
    public LogSeverity LogLevel;
    public ulong GuildId;
    public ulong MessageLogsChannelId;
    public ulong StorageChannelId;
    public ulong LogsChannelId;
    public string MacroPrefix;
    public ulong MutedRole;
    public ulong[] ModRoles;

    public static BotConfig Default => new BotConfig
    {
        Token = "YOUR_TOKEN",
        LogLevel = LogSeverity.Info,
        MacroPrefix = "!",
        ModRoles = Array.Empty<ulong>()
    };
}

public class LogSeverityConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => writer.WriteValue(value?.ToString());
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    => Enum.Parse<LogSeverity>(reader.Value?.ToString() ?? throw new InvalidOperationException("LogSeverity value is null."));
    public override bool CanConvert(Type objectType) => objectType == typeof(LogSeverity);
}