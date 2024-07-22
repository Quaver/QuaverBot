using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Discord;
using Discord.WebSocket;

namespace QuaverBot.Modules;

public static class Clean
{
    public static void InitClean(DiscordSocketClient client)
    {
        CleanTimer = new Timer(1000 * 60 * 30);
        CleanTimer.Elapsed += (sender, args) => CleanChannels(client);
        CleanTimer.Start();
    }

    private static CleanConfig Config => QuaverBot.Config.Clean;
    private static Timer? CleanTimer;

    public static async void CleanChannels(DiscordSocketClient client)
    {
        var channels = Config.Channels.Select(c => client.GetChannel(c)).Where(c => c != null);
        var after = DateTimeOffset.Now.AddHours(-Config.AfterHours);

        foreach (var channel in channels)
        {
            if (channel is not SocketTextChannel textChannel)
                continue;
            var messages = await textChannel.GetMessagesAsync().FlattenAsync();

            List<IMessage> toDelete = new List<IMessage>();
            foreach (var message in messages)
            {
                if (message.CreatedAt >= after)
                    continue;
                if (Config.IgnoreMessages.Contains(message.Id))
                    continue;
                if (Config.IgnoreUsers.Contains(message.Author.Id))
                    continue;
                if (message.Author is SocketGuildUser user && Config.IgnoreRoles.Any(r => user.Roles.Any(ur => ur.Id == r)))
                    continue;
                
                toDelete.Add(message);
            }

            if (toDelete.Count == 0)
                continue;

            await textChannel.DeleteMessagesAsync(toDelete);
        }
    }
}