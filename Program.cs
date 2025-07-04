using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace QuaverBot;

public class Program
{
    public static void Main(string[] args)
    {
        if (!File.Exists("config.json"))
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(BotConfig.Default, Formatting.Indented));
            Console.WriteLine("Please fill in the config.json file.");
            return;
        }
        BotConfig config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("config.json"));

        if (args.Length > 0 && args[0] == "regen")
        {
            File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
            Console.WriteLine("Regenerated config.json.");
            return;
        }

        Logger.LogLevel = config.LogLevel;

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

    restart:
        try
        {
            new QuaverBot(config).MainAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // "gotos are illegal": womp womp
            // simply uncrash yourself
            goto restart;
        }

    }
}