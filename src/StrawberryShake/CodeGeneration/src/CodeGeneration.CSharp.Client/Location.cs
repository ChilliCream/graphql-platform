using System;
using System.IO;
using static System.Environment;

namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class Location
{
    public Location(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public int Line { get; }

    public int Column { get; }
}

public static class DebugLog
{
    public static void Log(string message)
    {
        File.AppendAllText(
            Path.Combine(Environment.GetFolderPath(SpecialFolder.UserProfile), "berry.log"),
            message + Environment.NewLine);
    }
}