using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui;

internal static class AnsiAssertions
{
    /// <summary>
    /// Asserts the ANSI escape sequence for <paramref name="token"/>'s style
    /// appears in <paramref name="output"/>. A plain <see cref="TestConsole"/>
    /// strips markup entirely, so a wrong or missing token name would still
    /// leave every plain-text <c>Contains</c> assertion elsewhere green;
    /// <paramref name="output"/> must come from a console built with
    /// <c>.Colors(ColorSystem.TrueColor)</c> and <c>.EmitAnsiSequences()</c>.
    /// </summary>
    public static void AssertAnsiStyleApplied(string output, string token)
    {
        var style = ThemeTokens.GetStyle(token);
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix, output);
    }
}
