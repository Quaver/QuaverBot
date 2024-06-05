using SQLite;

namespace QuaverBot.Database;

public class DatabaseMute
{
    [PrimaryKey, Unique]
    public ulong DiscordId { get; set; }

    public long Until { get; set; }
}