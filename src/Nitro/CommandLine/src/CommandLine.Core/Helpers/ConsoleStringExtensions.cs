namespace ChilliCream.Nitro.CommandLine;

internal static class ConsoleStringExtensions
{
    public static string Space(this string str)
    {
        return $"{str} ";
    }

    public static string AsQuestion(this string str)
    {
        return $"{Glyphs.QuestionMark} [bold]{str}[/]";
    }
}
