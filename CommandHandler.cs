using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuaverBot.Modules;

namespace QuaverBot;

public class CommandHandler
{
    public DiscordSocketClient Client;
    public InteractionService InteractionService;

    private IServiceProvider services;

    public CommandHandler(DiscordSocketClient client)
    {
        Client = client;
        var config = new InteractionServiceConfig()
        {
            DefaultRunMode = RunMode.Async
        };
        InteractionService = new(Client, config);

        services = new ServiceCollection()
            .AddSingleton(client)
            .BuildServiceProvider();
    }

    public async Task InstallCommandsAsync()
    {
        Client.Ready += () =>
        {
            Task.Run(RegisterCommands);

            Mute.CheckMutedTimer = new System.Timers.Timer(1000 * 15);
            Mute.CheckMutedTimer.Elapsed += (s, e) => { Mute.CheckMuted(Client); };
            Mute.CheckMutedTimer.AutoReset = true;
            Mute.CheckMutedTimer.Start();

            return Task.CompletedTask;
        };
        InteractionService.SlashCommandExecuted += SlashCommandExecuted;
        await InteractionService.AddModulesAsync(assembly: System.Reflection.Assembly.GetExecutingAssembly(), services: services);
        Client.InteractionCreated += HandleInteraction;

        Client.MessageReceived += Macros.OnMessageReceived;
        Client.MessageReceived += (m) => Logging.OnMessageReceived(m, Client);
        Client.MessageDeleted += (m, c) => Logging.OnMessageDeleted(m, c, Client);
    }

    public async Task RegisterCommands()
    {
        try
        {
            foreach (var guild in Client.Guilds)
            {
                await InteractionService.RegisterCommandsToGuildAsync(guild.Id, true);
                Logger.Debug("Added commands to " + guild.Id, this);
            }
        }
        catch (Exception ex)
        {
            Logger.Critical(ex.Message, this, ex);
        }
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(Client, interaction);
            var result = await InteractionService.ExecuteCommandAsync(context, services);
        }
        catch (Exception ex)
        {
            Logger.Critical(ex.Message, this, ex);
        }
    }

    private async Task SlashCommandExecuted(SlashCommandInfo info, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    await context.Interaction.RespondAsync($"Unmet Precondition: {result.ErrorReason}");
                    break;
                case InteractionCommandError.UnknownCommand:
                    await context.Interaction.RespondAsync("Unknown command");
                    break;
                case InteractionCommandError.BadArgs:
                    await context.Interaction.RespondAsync("Invalid number or arguments");
                    break;
                case InteractionCommandError.Exception:
                    var exception = ((ExecuteResult)result).Exception;
                    await context.Interaction.RespondAsync($"Command exception: {result.ErrorReason}\n" +
                        $"```json\n{JsonConvert.SerializeObject(exception.InnerException, Formatting.Indented)}\n```");
                    break;
                case InteractionCommandError.Unsuccessful:
                    await context.Interaction.RespondAsync("Command could not be executed");
                    break;
                default:
                    await context.Interaction.RespondAsync(result.Error.ToString() + ": " + result.ErrorReason);
                    break;
            }
        }
    }
}