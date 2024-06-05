using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using QuaverBot;
using QuaverBot.Database;

namespace QuaverBot.Modules;

[Group("macros", "Manage macros")]
public class Macros : InteractionModuleBase<SocketInteractionContext>
{
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890_";

    [SlashCommand("add", "Add a macro, \\n gets replaced with a new line")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task AddMacroAsync(string name, string content)
    {
        if (name.Any(c => !AllowedChars.Contains(c)))
        {
            await RespondAsync("Invalid characters in macro name");
            return;
        }

        if (AddMacro(name, content))
            await RespondAsync($"Added macro {name}");
        else
            await RespondAsync($"Macro {name} already exists");
    }

    [SlashCommand("remove", "Remove a macro")]
    [RequireUserPermission(GuildPermission.ManageMessages)]
    public async Task RemoveMacroAsync([Autocomplete(typeof(MacroNameAutocomplete))] string name)
    {
        Macro? macro = DatabaseManager.Connection?.Find<Macro>(name);
        if (macro == null)
        {
            await RespondAsync($"Macro {name} does not exist");
            return;
        }

        DatabaseManager.Connection?.Delete(macro);
        await RespondAsync($"Removed macro {name}");
    }

    public static async Task OnMessageReceived(SocketMessage message)
    {
        var prefix = QuaverBot.Config.MacroPrefix;
        if (message is not SocketUserMessage userMessage || message.Author.IsBot) return;
        if (!userMessage.Content.StartsWith(prefix)) return;

        string macroName = userMessage.Content.Substring(prefix.Length);
        string? macroContent = GetMacro(macroName);
        if (macroContent == null) return;

        await message.Channel.SendMessageAsync(macroContent);
    }

    public static bool AddMacro(string name, string content)
    {
        Macro? macro = DatabaseManager.Connection?.Find<Macro>(name);
        if (macro != null) return false;

        content = content.Replace("\\n", "\n");
        DatabaseManager.Connection?.Insert(new Macro
        {
            Name = name,
            Content = content
        });
        return true;
    }

    public static string? GetMacro(string name)
    {
        Macro? macro = DatabaseManager.Connection?.Find<Macro>(name);
        return macro?.Content;
    }

    public class MacroNameAutocomplete : AutocompleteHandler
    {
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
        {
            var macros = DatabaseManager.Connection?.Table<Macro>().Select(m => m.Name).Where(s => s.StartsWith((string)autocompleteInteraction.Data.Current.Value)).ToList();
            if (macros == null) return Task.FromResult(AutocompletionResult.FromError(InteractionCommandError.Unsuccessful, "No macros found"));

            List<AutocompleteResult> results = new();
            foreach (string macro in macros)
            {
                results.Add(new AutocompleteResult(macro, macro));
            }

            return Task.FromResult(AutocompletionResult.FromSuccess(results.Take(25)));

        }
    }
}