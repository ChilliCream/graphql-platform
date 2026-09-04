using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Theming;

public sealed class DefaultThemeTests
{
    /// <summary>
    /// Terminals draw bold text with a distinct (or synthesized) font face
    /// whose box-drawing glyph metrics can drift from the regular face, so a
    /// pane's focus is shown by box weight (see
    /// <see cref="ChilliCream.Nitro.CommandLine.Tui.Widgets.PaneBorders"/>),
    /// never by bolding its border or accent tokens. This guards every
    /// current and future border/status token so a bold focused accent
    /// cannot silently regress the broken-frame bug.
    /// </summary>
    [Fact]
    public void FocusedBorderAndStatusTokens_Should_NeverCarryBold()
    {
        // arrange
        var offenders = DefaultTheme.Tokens
            .Where(pair => pair.Key.EndsWith(".focused", StringComparison.Ordinal))
            .Where(pair => pair.Key.Contains("border", StringComparison.Ordinal)
                || pair.Key.Contains("status", StringComparison.Ordinal))
            .Where(pair => pair.Value.Decoration.HasFlag(Decoration.Bold))
            .Select(pair => pair.Key)
            .ToArray();

        // assert
        Assert.Empty(offenders);
    }
}
