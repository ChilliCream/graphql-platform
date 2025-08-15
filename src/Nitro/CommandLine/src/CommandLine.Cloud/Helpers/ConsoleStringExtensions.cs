namespace ChilliCream.Nitro.CommandLine.Cloud;

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

    public static string AsQuestion(this string str)
    {
        return $"{Glyphs.QuestionMark} [bold]{str}[/]";
    }

    public static string AsIcon(this bool value)
    {
        return value ? Glyphs.Check : Glyphs.Cross;
    }
}

internal static class Glyphs
{
    public const string Check = "[green bold]✓[/]";

    public const string Cross = "[red bold]✕[/]";

    public const string QuestionMark = "[lime bold]?[/]";

    public const string ExclamationMark = "[yellow bold]![/]";
}
