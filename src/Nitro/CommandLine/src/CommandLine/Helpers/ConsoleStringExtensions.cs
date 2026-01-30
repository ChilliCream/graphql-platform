namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class ConsoleRenderExtensions
{
    public static string AsSchemaCoordinate(this string str)
    {
        return $"[bold]{str.EscapeMarkup()}[/]";
    }

    public static string Space(this string str)
    {
        return $"{str} ";
    }

    public static string AsSyntax(this string str)
    {
        return $"[bold]{str.EscapeMarkup()}[/]";
    }

    public static string AsDescription(this string str)
    {
        return $"[italic dim]{str.EscapeMarkup()}[/]";
    }

    public static string AsHighlight(this string str)
    {
        return $"[bold blue]{str.EscapeMarkup()}[/]";
    }

    public static string AsCommand(this string str) => $"`{str.AsHighlight()}`";

    public static string AsIcon(this bool value)
    {
        return value ? Glyphs.Check : Glyphs.Cross;
    }

    public static string AsQuestion(this string str)
    {
        return $"{Glyphs.QuestionMark} [bold]{str}[/]";
    }
}
