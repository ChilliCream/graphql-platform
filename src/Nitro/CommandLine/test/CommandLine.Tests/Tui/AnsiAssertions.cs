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

    /// <summary>
    /// Asserts <paramref name="token"/>'s style opens immediately before
    /// <paramref name="text"/> in <paramref name="output"/>. Unlike
    /// <see cref="AssertAnsiStyleApplied"/>, this pins the escape sequence
    /// to the known rendered text instead of anywhere in the frame, so it
    /// still fails when another element in the frame happens to share the
    /// same style. <paramref name="output"/> must come from a console built
    /// with <c>.Colors(ColorSystem.TrueColor)</c> and
    /// <c>.EmitAnsiSequences()</c>.
    /// </summary>
    public static void AssertAnsiStylePrefixesText(string output, string token, string text)
    {
        var style = ThemeTokens.GetStyle(token);
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix + text, output);
    }
}
