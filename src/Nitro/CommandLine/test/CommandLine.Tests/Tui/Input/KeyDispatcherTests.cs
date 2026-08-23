using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Input;

public sealed class KeyDispatcherTests
{
    private static ConsoleKeyInfo KeyInfo(char keyChar, ConsoleKey key, ConsoleModifiers modifiers = ConsoleModifiers.None) =>
        new(
            keyChar,
            key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));

    [Fact]
    public void Dispatch_Should_PreferModeTable_When_KeyBoundInBothTables()
    {
        // arrange
        var globalMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested())]);
        var modeMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.RefreshRequested())]);
        var dispatcher = new KeyDispatcher(globalMap);

        // act
        var message = dispatcher.Dispatch(KeyInfo('q', ConsoleKey.Q), modeMap);

        // assert
        Assert.IsType<TuiMessage.RefreshRequested>(message);
    }

    [Fact]
    public void Dispatch_Should_FallBackToGlobalTable_When_KeyUnboundInModeTable()
    {
        // arrange
        var globalMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested())]);
        var modeMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j'), () => new TuiMessage.MoveCursor(CursorDirection.Down))]);
        var dispatcher = new KeyDispatcher(globalMap);

        // act
        var message = dispatcher.Dispatch(KeyInfo('q', ConsoleKey.Q), modeMap);

        // assert
        Assert.IsType<TuiMessage.QuitRequested>(message);
    }

    [Fact]
    public void Dispatch_Should_UseGlobalTable_When_ModeTableIsNull()
    {
        // arrange
        var globalMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested())]);
        var dispatcher = new KeyDispatcher(globalMap);

        // act
        var message = dispatcher.Dispatch(KeyInfo('q', ConsoleKey.Q), modeKeyMap: null);

        // assert
        Assert.IsType<TuiMessage.QuitRequested>(message);
    }

    [Fact]
    public void Dispatch_Should_ReturnNull_When_KeyUnboundInBothTables()
    {
        // arrange
        var globalMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested())]);
        var modeMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j'), () => new TuiMessage.MoveCursor(CursorDirection.Down))]);
        var dispatcher = new KeyDispatcher(globalMap);

        // act
        var message = dispatcher.Dispatch(KeyInfo('x', ConsoleKey.X), modeMap);

        // assert
        Assert.Null(message);
    }

    [Fact]
    public void Dispatch_Should_ResolveDefaultGlobalKeys()
    {
        // arrange
        var dispatcher = new KeyDispatcher(KeyMap.CreateDefaultGlobal());

        // act
        var message = dispatcher.Dispatch(KeyInfo('j', ConsoleKey.J), modeKeyMap: null);

        // assert
        Assert.Equal(CursorDirection.Down, Assert.IsType<TuiMessage.MoveCursor>(message).Direction);
    }
}
