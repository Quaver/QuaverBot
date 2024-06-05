using SQLite;

namespace QuaverBot.Database;

public static class DatabaseManager
{
    public static SQLiteConnection? Connection;
    public static void Initialize()
    {
        Connection = new SQLiteConnection("quaverbot.db");

        Connection.CreateTable<Macro>();
        Connection.CreateTable<ModHistory>();
        Connection.CreateTable<DatabaseMute>();
    }
}