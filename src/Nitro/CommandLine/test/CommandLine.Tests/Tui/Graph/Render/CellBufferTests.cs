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

    private static void Write(CellBuffer buffer, string text, int y)
    {
        for (var x = 0; x < text.Length; x++)
        {
            buffer.Set(x, y, text[x]);
        }
    }
}
