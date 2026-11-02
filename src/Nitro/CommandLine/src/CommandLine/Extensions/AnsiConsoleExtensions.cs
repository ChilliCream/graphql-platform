namespace ChilliCream.Nitro.CommandLine;

internal static class AnsiConsoleExtensions
{
    public static void WriteErrorLine(this IAnsiConsole console, string message)
    {
        console.MarkupLine($"[red]{message}[/]");
    }
}
