using System.Text.RegularExpressions;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal static partial class InteractiveTerminalSnapshot
{
    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex AnsiRegex();

    public static string Format(CommandBuilder host)
    {
        var visible = NormalizeVisible(host.Output);
        var ansi = EscapeControlSequences(host.Output);
        var stderr = NormalizeText(host.StdErr);

        if (string.IsNullOrEmpty(stderr))
        {
            stderr = "<empty>";
        }

        return $"""
                [visible]
                {visible}

                [ansi]
                {ansi}

                [stderr]
                {stderr}
                """;
    }

    private static string NormalizeVisible(string output)
    {
        var noAnsi = AnsiRegex().Replace(output, string.Empty);
        var withoutCarriageReturns = noAnsi.Replace("\r", string.Empty, StringComparison.Ordinal);
        return NormalizeText(withoutCarriageReturns);
    }

    private static string EscapeControlSequences(string output)
    {
        var escaped = output
            .Replace("\u001b", "<ESC>", StringComparison.Ordinal)
            .Replace("\r", "<CR>", StringComparison.Ordinal)
            .Replace("\n", "<LF>\n", StringComparison.Ordinal);

        return NormalizeText(escaped);
    }

    private static string NormalizeText(string text)
    {
        var lines = text
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(static line => line.TrimEnd())
            .ToArray();

        return string.Join("\n", lines).TrimEnd();
    }
}
