using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Runtime;

public sealed class TerminalSessionTests
{
    [Fact]
    public void Constructor_Should_EnterAlternateScreen_And_HideCursor()
    {
        // arrange
        var console = new TestConsole { EmitAnsiSequences = true };

        // act
        using var session = new TerminalSession(console);

        // assert
        Assert.Contains("[?1049h", console.Output);
    }

    [Fact]
    public void Dispose_Should_ExitAlternateScreen_And_ShowCursor()
    {
        // arrange
        var console = new TestConsole { EmitAnsiSequences = true };
        var session = new TerminalSession(console);

        // act
        session.Dispose();

        // assert
        Assert.Contains("[?1049l", console.Output);
    }

    [Fact]
    public void Dispose_Should_BeIdempotent_When_CalledMoreThanOnce()
    {
        // arrange
        var console = new TestConsole { EmitAnsiSequences = true };
        var session = new TerminalSession(console);
        session.Dispose();
        var outputAfterFirstDispose = console.Output;

        // act
        session.Dispose();

        // assert
        Assert.Equal(outputAfterFirstDispose, console.Output);
    }
}
