using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using QuaverBot.Database;
using QuaverBot.Modules;

namespace QuaverBot;

public class QuaverBot
{
    public static BotConfig Config;

    public DiscordSocketClient Client;
    public CommandHandler CommandHandler;

    public QuaverBot(BotConfig args)
    {
        Config = args;

        DatabaseManager.Initialize();

        var clientConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            MessageCacheSize = 1024,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
        };

        Client = new DiscordSocketClient(clientConfig);
        Client.Log += Logger.Log;
        CommandHandler = new(Client);

        Client.UserJoined += Mute.OnUserJoined;
    }

    public async Task MainAsync()
    {
        if (string.IsNullOrEmpty(Config.Token))
        {
            Logger.Critical("Token is empty", this);
            return;
        }

        await CommandHandler.InstallCommandsAsync();

        await Client.LoginAsync(TokenType.Bot, Config.Token);
        await Client.StartAsync();
        await Task.Delay(-1);
    }
}
