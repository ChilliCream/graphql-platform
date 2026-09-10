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

    [Fact]
    public void CombineHints_Should_AppendGlobalHints_After_ContextHints()
    {
        // arrange
        var globalMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested(), new KeyHint("q", "quit"))]);
        var dispatcher = new KeyDispatcher(globalMap);
        KeyHint[] contextHints = [new KeyHint("j", "move")];

        // act
        var combined = dispatcher.CombineHints(contextHints, suppressedGlobalHints: []);

        // assert
        Assert.Equal([new KeyHint("j", "move"), new KeyHint("q", "quit")], combined);
    }

    [Fact]
    public void CombineHints_Should_NotRepeat_When_GlobalHintAlreadyInContextHints()
    {
        // arrange
        var globalMap = new KeyMap(
            [new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested(), new KeyHint("q", "quit"))]);
        var dispatcher = new KeyDispatcher(globalMap);
        KeyHint[] contextHints = [new KeyHint("q", "quit")];

        // act
        var combined = dispatcher.CombineHints(contextHints, suppressedGlobalHints: []);

        // assert
        Assert.Equal([new KeyHint("q", "quit")], combined);
    }

    [Fact]
    public void CombineHints_Should_DropSuppressedGlobalHints()
    {
        // arrange: a mode overrides a global gesture its current state makes
        // inert (see ITuiMode.SuppressedGlobalHints), so the footer must not
        // advertise it even though the global table still binds it.
        var globalMap = new KeyMap(
        [
            new KeyBinding(new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'), () => new TuiMessage.QuitRequested(), new KeyHint("q", "quit")),
            new KeyBinding(new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'), () => new TuiMessage.RefreshRequested(), new KeyHint("r", "refresh"))
        ]);
        var dispatcher = new KeyDispatcher(globalMap);

        // act
        var combined = dispatcher.CombineHints(contextHints: [], suppressedGlobalHints: [new KeyHint("r", "refresh")]);

        // assert
        Assert.Equal([new KeyHint("q", "quit")], combined);
    }
}
