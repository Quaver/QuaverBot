using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace QuaverBot.Modules;

public class Clear : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("clear", "Clear messages from a channel")]
    [RequireMod]
    public async Task ClearAsync(int amount, IUser? user = null)
    {
        if (amount < 1)
        {
            await RespondAsync("Amount must be at least 1.");
            return;
        }

        if (Context.Channel is not ITextChannel textChannel)
        {
            await RespondAsync("Channel is not a text channel.");
            return;
        }

        await DeferAsync();

        List<IMessage> messages = new List<IMessage>();
        IMessage? oldestMessage = null;
        while (messages.Count < amount && ((!messages.Any()) || messages.LastOrDefault()?.Timestamp > DateTimeOffset.Now.AddDays(-14)))
        {
            var fetched = oldestMessage == null ?
                await textChannel.GetMessagesAsync(100).FlattenAsync() :
                await textChannel.GetMessagesAsync(oldestMessage, Direction.Before, 100).FlattenAsync();

            if (!fetched.Any())
                break;

            oldestMessage = fetched.Last();
            messages.AddRange(fetched.Where(m => user == null || m.Author.Id == user.Id));
        }

        if (messages.Count == 0)
        {
            await FollowupAsync("No messages found.");
            return;
        }

        messages = messages
            .Where(m =>
                m.Timestamp > DateTimeOffset.Now.AddDays(-14) &&
                m.Interaction?.Id != Context.Interaction.Id)
            .Take(amount)
            .ToList();

        await textChannel.DeleteMessagesAsync(messages);

        await FollowupAsync($"Deleted {messages.Count} messages.");
    }
}