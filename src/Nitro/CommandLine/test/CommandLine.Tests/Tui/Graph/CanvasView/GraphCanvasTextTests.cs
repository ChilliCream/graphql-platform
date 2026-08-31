using ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.CanvasView;

public sealed class GraphCanvasTextTests
{
    [Fact]
    public void Encode_Should_ReplaceNonSingleCellRunes_When_TextContainsUnicode()
    {
        // arrange
        const string value = "A😀界e\u0301\uD83D";

        // act
        var encoded = GraphCanvasText.Encode(value);
        var truncated = GraphCanvasText.Truncate(value, 4);

        // assert
        new[] { encoded, truncated }.MatchInlineSnapshots(["A??e??", "A??…"]);
    }
}
