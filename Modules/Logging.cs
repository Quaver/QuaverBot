using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using QuaverBot.Database;

namespace QuaverBot.Modules;

public static class Logging
{
    const string ATTACHMENT_DIR = "attachments";

    public static void Init()
    {
        Directory.CreateDirectory(ATTACHMENT_DIR);

        CleanupTimer = new Timer(1000 * 60 * 15); // 15m
        CleanupTimer.Elapsed += (sender, args) => CleanupCache();
        CleanupTimer.Start();
    }

    private static HttpClient HttpClient { get; } = new();

    private static string AttachmentFile(ulong id)
    {
        return $"{ATTACHMENT_DIR}/{id}";
    }

    private static Timer? CleanupTimer;
    private static void CleanupCache()
    {
        var files = Directory.GetFiles($"{ATTACHMENT_DIR}").Select(x => ulong.Parse(Path.GetFileName(x))).OrderByDescending(x => x);
        long size = 0;
        foreach (var file in files)
        {
            size += new FileInfo(AttachmentFile(file)).Length;

            if (size > QuaverBot.Config.Log.MaxCacheSize)
            {
                File.Delete(AttachmentFile(file));
            }
        }
    }

    public static async Task OnMessageReceived(SocketMessage message, DiscordSocketClient client)
    {
        if (message.Author.IsBot) return;
        if (!QuaverBot.Config.Log.CacheAttachments) return;

        if (message.Attachments.Count > 0)
        {
            foreach (var attachment in message.Attachments)
            {
                var response = await HttpClient.GetAsync(attachment.Url);
                if (response.IsSuccessStatusCode)
                {
                    var buffer = await response.Content.ReadAsByteArrayAsync();

                    await File.WriteAllBytesAsync(AttachmentFile(attachment.Id), buffer);
                }
            }
        }
    }

    public static async Task OnMessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, DiscordSocketClient client)
    {
        if (!message.HasValue) return;
        if (message.Value.Author.IsBot) return;

        List<FileAttachment> attachments = new();
        string attachmentsField = "";

        if (QuaverBot.Config.Log.CacheAttachments)
        {
            for (int i = 0; i < message.Value.Attachments.Count; i++)
            {
                var attachment = message.Value.Attachments.ElementAt(i);
                bool exists = File.Exists(AttachmentFile(attachment.Id));
                if (exists)
                {
                    attachments.Add(new FileAttachment(AttachmentFile(attachment.Id), attachment.Filename));
                }
                attachmentsField += $"[{i}] {attachment.Filename} ({exists})";
            }
        }

        if (client.GetChannel(QuaverBot.Config.Log.ChannelId) is not ITextChannel logsChannel)
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

        embed.WithAuthor(message.Value.Author);
        embed.WithDescription(message.Value.Content);
        embed.AddField("Channel", $"<#{channel.Id}>", false);

        if (attachments.Count == 0 && attachmentsField == "")
        {
            await logsChannel.SendMessageAsync(embed: embed.Build());
        }
        else
        {
            embed.AddField("Attachments", attachmentsField, false);
            await logsChannel.SendFilesAsync(attachments, embed: embed.Build());
        }
    }

    public static async Task LogMute(ulong user, ulong mod, string? reason, TimeSpan? duration, DiscordSocketClient client)
    {
        if (QuaverBot.Config.ModlogChannelId == 0) return;

        if (client.GetChannel(QuaverBot.Config.ModlogChannelId) is not ITextChannel logsChannel)
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
        if (QuaverBot.Config.ModlogChannelId == 0) return;

        if (client.GetChannel(QuaverBot.Config.ModlogChannelId) is not ITextChannel logsChannel)
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