using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.Render;

public sealed class CellBufferTests
{
    [Fact]
    public void Render_Should_ClipTheRequestedViewport_AndReturnRenderable()
    {
        // arrange
        var buffer = new CellBuffer(5, 3);
        Write(buffer, "abcde", 0);
        Write(buffer, "fghij", 1);
        Write(buffer, "klmno", 2);
        var viewport = new CanvasViewport(1, 1, 3, 2);

        // act
        var renderable = buffer.Render(viewport);
        var console = new TestConsole().Width(3).Height(2);
        console.Write(renderable);

        // assert
        Assert.IsAssignableFrom<IRenderable>(renderable);
        Assert.Equal("ghi" + Environment.NewLine + "lmn", buffer.ToText(viewport));
        Assert.Equal("ghi" + Environment.NewLine + "lmn", console.Output);
    }

    [Fact]
    public void Render_Should_ClipStyledRunsToTheAvailableWidth()
    {
        // arrange
        var buffer = new CellBuffer(5, 1);
        var red = new Style(Color.Red);
        var blue = new Style(Color.Blue);
        buffer.Set(0, 0, 'a', red);
        buffer.Set(1, 0, 'b', red);
        buffer.Set(2, 0, 'c', blue);
        buffer.Set(3, 0, 'd', blue);
        buffer.Set(4, 0, 'e', Style.Plain);
        var viewport = new CanvasViewport(0, 0, 5, 1);

        // act
        var segments = buffer.GetSegments(viewport, 3)
            .Select(t => new StyledRun(t.Text, t.Style))
            .ToArray();
        var console = new TestConsole().Width(3).Height(1);
        console.Write(buffer.Render(viewport));

        // assert
        Assert.Equal([new StyledRun("ab", red), new StyledRun("c", blue)], segments);
        Assert.Equal("abc", console.Output);
    }

    [Fact]
    public void Connect_Should_KeepOwnersAndWinningContributionWhenInputOrderChanges()
    {
        // arrange
        var blocks = new object();
        var parent = new object();
        var overrideOwner = new object();
        var normal = ConnectSharedCell(blocks, parent, overrideOwner, reverse: false);

        // act
        var reversed = ConnectSharedCell(blocks, parent, overrideOwner, reverse: true);

        // assert
        Assert.Equal(new[] { blocks, parent, overrideOwner }, normal.Owners.ToArray());
        Assert.Equal(new[] { overrideOwner, parent, blocks }, reversed.Owners.ToArray());
        Assert.Equal(new StyledCell('┆', new Style(Color.Red, null, Decoration.Dim), Decoration.Dim), Describe(normal));
        Assert.Equal(Describe(normal), Describe(reversed));
    }

    [Fact]
    public void SetArrow_Should_KeepHigherPriorityStrokeGlyph_When_ArrowHasLowerPriority()
    {
        // arrange
        var buffer = new CellBuffer(1, 1);
        var owner = new object();
        var style = new Style(Color.Red, null, Decoration.Dim);
        buffer.Connect(0, 0, CanvasDirections.Left | CanvasDirections.Right, style, owner, true, 3, 0);

        // act
        buffer.SetArrow(0, 0, '▶', new Style(Color.Blue), owner, false, 1, 0);
        var cell = buffer.Get(0, 0);

        // assert
        Assert.Equal('┄', cell.Glyph);
        Assert.Equal(style, cell.Style);
    }

    private static void Write(CellBuffer buffer, string text, int y)
    {
        for (var x = 0; x < text.Length; x++)
        {
            buffer.Set(x, y, text[x]);
        }
    }

    private static CanvasCell ConnectSharedCell(object blocks, object parent, object overrideOwner, bool reverse)
    {
        // arrange
        var buffer = new CellBuffer(1, 1);
        var contributions = new[]
        {
            (CanvasDirections.Up, new Style(Color.Blue), blocks, false, 1, 2),
            (CanvasDirections.Down, new Style(Color.Green), parent, false, 2, 1),
            (CanvasDirections.Up, new Style(Color.Red, null, Decoration.Dim), overrideOwner, true, 3, 0)
        };

        // act
        foreach (var contribution in reverse ? contributions.Reverse() : contributions)
        {
            buffer.Connect(
                0,
                0,
                contribution.Item1,
                contribution.Item2,
                contribution.Item3,
                contribution.Item4,
                contribution.Item5,
                contribution.Item6);
        }

        // assert
        return buffer.Get(0, 0);
    }

    private static StyledCell Describe(CanvasCell cell)
        => new(cell.Glyph, cell.Style, cell.Style.Decoration);

    private sealed record StyledRun(string Text, Style Style);

    private sealed record StyledCell(char Glyph, Style Style, Decoration Decoration);
}
