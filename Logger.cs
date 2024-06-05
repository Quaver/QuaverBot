using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace QuaverBot;

public static class Logger
{
    public static void Log(string message, LogSeverity severity, object? source = null, Exception? exception = null)
    {
        if (severity > LogLevel) return;

        if (LogToConsole) LogConsole(message, severity, source, exception);
    }

    public static Task Log(LogMessage logMessage)
    {
        Log(logMessage.Message, logMessage.Severity, logMessage.Source, logMessage.Exception);
        return Task.CompletedTask;
    }

    private static void LogConsole(string message, LogSeverity severity, object? source, Exception? exception)
    {
        Console.ForegroundColor = SeverityToColor(severity);
        Console.Write($"[{DateTime.Now:HH:mm:ss}] [{severity}] ");
        if (source != null) Console.Write($"[{ObjectToString(source)}] ");
        Console.WriteLine(message);
        if (exception != null) Console.WriteLine(exception);
        Console.ResetColor();
    }

    private static string ObjectToString(object? obj) => obj switch
    {
        null => "null",
        string s => s,
        _ => obj.GetType().ToString()
    };

    public static ConsoleColor SeverityToColor(LogSeverity severity) => severity switch
    {
        LogSeverity.Critical => ConsoleColor.Red,
        LogSeverity.Error => ConsoleColor.DarkRed,
        LogSeverity.Warning => ConsoleColor.Yellow,
        LogSeverity.Info => ConsoleColor.White,
        LogSeverity.Verbose => ConsoleColor.Gray,
        LogSeverity.Debug => ConsoleColor.DarkGray,
        _ => ConsoleColor.White
    };

    public static void Critical(string message, object? source = null, Exception? exception = null) => 
        Log(message, LogSeverity.Critical, source, exception);
    public static void Error(string message, object? source = null, Exception? exception = null) => 
        Log(message, LogSeverity.Error, source, exception);
    public static void Warning(string message, object? source = null, Exception? exception = null) => 
        Log(message, LogSeverity.Warning, source, exception);
    public static void Info(string message, object? source = null, Exception? exception = null) => 
        Log(message, LogSeverity.Info, source, exception);
    public static void Verbose(string message, object? source = null, Exception? exception = null) => 
        Log(message, LogSeverity.Verbose, source, exception);
    public static void Debug(string message, object? source = null, Exception? exception = null) => 
        Log(message, LogSeverity.Debug, source, exception);

    public static LogSeverity LogLevel { get; set; } = LogSeverity.Info;
    public static bool LogToConsole { get; set; } = true;
}
