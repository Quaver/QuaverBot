using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using QuaverBot.Database;

namespace QuaverBot.Modules;

public static class Logging
{
    private static HttpClient HttpClient { get; } = new();

    private static Dictionary<ulong, ulong> AttachmentStorage { get; } = new();

    public static async Task OnMessageReceived(SocketMessage message, DiscordSocketClient client)
    {
        if (message.Author.IsBot) return;
        if (QuaverBot.Config.StorageChannelId == 0) return;

        if (message.Attachments.Count > 0)
        {
            List<FileAttachment> attachments = new();

            foreach (var attachment in message.Attachments)
            {
                var response = await HttpClient.GetAsync(attachment.Url);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    attachments.Add(new FileAttachment(stream, attachment.Filename));
                }
            }

            if (attachments.Count > 0)
            {
                if (client.GetChannel(QuaverBot.Config.StorageChannelId) is not ITextChannel storageChannel)
                {
                    Logger.Error("Storage channel not found.");
                    return;
                }

                var storageMessage = await storageChannel.SendFilesAsync(attachments);

                AttachmentStorage.Add(message.Id, storageMessage.Id);
            }
        }
    }

    public static async Task OnMessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, DiscordSocketClient client)
    {
        if (message.HasValue && message.Value.Author.IsBot) return;

        List<FileAttachment> attachments = new();

        var exists = AttachmentStorage.TryGetValue(message.Id, out ulong attachmentMsgId);

        if (QuaverBot.Config.StorageChannelId != 0 && exists)
        {
            if (client.GetChannel(QuaverBot.Config.StorageChannelId) is not ITextChannel storageChannel)
            {
                Logger.Error("Storage channel not found.");
                return;
            }

            var storageMessage = await storageChannel.GetMessageAsync(attachmentMsgId);

            if (storageMessage is not null)
            {
                foreach (var attachment in storageMessage.Attachments)
                {
                    var response = await HttpClient.GetAsync(attachment.Url);
                    if (response.IsSuccessStatusCode)
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        attachments.Add(new FileAttachment(stream, attachment.Filename));
                    }
                }
            }

            AttachmentStorage.Remove(message.Id);
        }

        if (attachments.Count == 0 && !message.HasValue) return;

        if (client.GetChannel(QuaverBot.Config.MessageLogsChannelId) is not ITextChannel logsChannel)
        {
            Logger.Error("Logs channel not found.");
            return;
        }

        var embed = new EmbedBuilder
        {
            Title = "Message Deleted",
            Color = Color.Red,
            Timestamp = DateTimeOffset.Now
        };

        embed.AddField("Channel", $"<#{channel.Id}>", true);

        if (message.HasValue)
        {
            embed.WithAuthor(message.Value.Author);

            embed.WithDescription(message.Value.Content);
        }

        if (QuaverBot.Config.StorageChannelId == 0)
            await logsChannel.SendMessageAsync(embed: embed.Build());
        else
            await logsChannel.SendFilesAsync(attachments, embed: embed.Build());
    }

    public static async Task LogMute(ulong user, ulong mod, string? reason, TimeSpan? duration, DiscordSocketClient client)
    {
        if (QuaverBot.Config.LogsChannelId == 0) return;

        if (client.GetChannel(QuaverBot.Config.LogsChannelId) is not ITextChannel logsChannel)
        {
            Logger.Error("Logs channel not found.");
            return;
        }

        var embed = new EmbedBuilder
        {
            Title = "Mute",
            Color = Color.Orange,
            Timestamp = DateTimeOffset.Now
        };

        embed.AddField("User", $"<@{user}>", true);
        embed.AddField("Moderator", $"<@{mod}>", true);
        embed.AddField("Duration", duration != null ? Mute.FormatTime(duration.Value) : "Permanent");
        embed.AddField("Reason", reason ?? "No reason provided.", true);

        await logsChannel.SendMessageAsync(embed: embed.Build());
    }

    public static async Task LogUnmute(ulong user, ulong mod, string? reason, DiscordSocketClient client)
    {
        if (QuaverBot.Config.LogsChannelId == 0) return;

        if (client.GetChannel(QuaverBot.Config.LogsChannelId) is not ITextChannel logsChannel)
        {
            Logger.Error("Logs channel not found.");
            return;
        }

        var embed = new EmbedBuilder
        {
            Title = "Unmute",
            Color = Color.Green,
            Timestamp = DateTimeOffset.Now
        };

        embed.AddField("User", $"<@{user}>", true);
        embed.AddField("Moderator", $"<@{mod}>", true);
        embed.AddField("Reason", reason ?? "No reason provided.");

        await logsChannel.SendMessageAsync(embed: embed.Build());
    }
}