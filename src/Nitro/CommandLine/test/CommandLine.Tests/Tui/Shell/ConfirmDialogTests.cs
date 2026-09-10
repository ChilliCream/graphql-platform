using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

public sealed class ConfirmDialogTests
{
    private static TuiMessage? Resolve(KeyMap map, ConsoleKey key, char keyChar, ConsoleModifiers modifiers = ConsoleModifiers.None) =>
        map.TryResolve(new KeyChord(key, modifiers, keyChar), out var message) ? message : null;

    [Fact]
    public void KeyMap_Should_ResolveY_ToConfirmQuit()
    {
        // arrange
        var dialog = new ConfirmDialog("Quit? (y/n)");

        // act
        var message = Resolve(dialog.KeyMap, ConsoleKey.Y, 'y');

        // assert
        Assert.IsType<TuiMessage.ConfirmQuit>(message);
    }

    [Fact]
    public void KeyMap_Should_ResolveN_ToCancelQuit()
    {
        // arrange
        var dialog = new ConfirmDialog("Quit? (y/n)");

        // act
        var message = Resolve(dialog.KeyMap, ConsoleKey.N, 'n');

        // assert
        Assert.IsType<TuiMessage.CancelQuit>(message);
    }

    [Fact]
    public void KeyMap_Should_ResolveEscape_ToCancelQuit()
    {
        // arrange
        var dialog = new ConfirmDialog("Quit? (y/n)");

        // act
        var message = Resolve(dialog.KeyMap, ConsoleKey.Escape, '');

        // assert
        Assert.IsType<TuiMessage.CancelQuit>(message);
    }

    [Fact]
    public void KeyMap_Should_ReturnNull_When_KeyUnbound()
    {
        // arrange
        var dialog = new ConfirmDialog("Quit? (y/n)");

        // act
        var message = Resolve(dialog.KeyMap, ConsoleKey.Q, 'q');

        // assert
        Assert.Null(message);
    }

    [Fact]
    public void Render_Should_IncludeMessage()
    {
        // arrange
        var dialog = new ConfirmDialog("Quit? (y/n)");
        var console = new TestConsole().Width(40).Height(10);

        // act
        console.Write(dialog.Render(40, 10));

        // assert
        Assert.Contains("Quit? (y/n)", console.Output);
    }
}
