using SQLite;

namespace QuaverBot.Database;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
public class Macro
{
    [PrimaryKey, Unique]
    public string Name { get; set; }

    public string Content { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.