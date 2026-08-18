using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Input;

public sealed class KeyMapTests
{
    [Fact]
    public void TryResolve_Should_ReturnMessage_When_ChordIsBound()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a');
        var keyMap = new KeyMap([new KeyBinding(chord, () => new TuiMessage.RefreshRequested())]);

        // act
        var resolved = keyMap.TryResolve(chord, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.RefreshRequested>(message);
    }

    [Fact]
    public void TryResolve_Should_ReturnFalse_When_ChordIsUnbound()
    {
        // arrange
        var bound = new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a');
        var unbound = new KeyChord(ConsoleKey.B, ConsoleModifiers.None, 'b');
        var keyMap = new KeyMap([new KeyBinding(bound, () => new TuiMessage.RefreshRequested())]);

        // act
        var resolved = keyMap.TryResolve(unbound, out var message);

        // assert
        Assert.False(resolved);
        Assert.Null(message);
    }

    [Fact]
    public void TryResolve_Should_DistinguishChords_By_Modifiers()
    {
        // arrange
        var lower = new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g');
        var upper = new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G');
        var keyMap = new KeyMap(
        [
            new KeyBinding(lower, () => new TuiMessage.MoveToEdge(EdgeTarget.Top)),
            new KeyBinding(upper, () => new TuiMessage.MoveToEdge(EdgeTarget.Bottom))
        ]);

        // act
        keyMap.TryResolve(lower, out var lowerMessage);
        keyMap.TryResolve(upper, out var upperMessage);

        // assert
        Assert.Equal(EdgeTarget.Top, Assert.IsType<TuiMessage.MoveToEdge>(lowerMessage).Edge);
        Assert.Equal(EdgeTarget.Bottom, Assert.IsType<TuiMessage.MoveToEdge>(upperMessage).Edge);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapJAndDownArrow_ToMoveCursorDown()
    {
        AssertMovesCursor(new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j'), CursorDirection.Down);
        AssertMovesCursor(new KeyChord(ConsoleKey.DownArrow, ConsoleModifiers.None, '\0'), CursorDirection.Down);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapKAndUpArrow_ToMoveCursorUp()
    {
        AssertMovesCursor(new KeyChord(ConsoleKey.K, ConsoleModifiers.None, 'k'), CursorDirection.Up);
        AssertMovesCursor(new KeyChord(ConsoleKey.UpArrow, ConsoleModifiers.None, '\0'), CursorDirection.Up);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapHAndLeftArrow_ToMoveCursorLeft()
    {
        AssertMovesCursor(new KeyChord(ConsoleKey.H, ConsoleModifiers.None, 'h'), CursorDirection.Left);
        AssertMovesCursor(new KeyChord(ConsoleKey.LeftArrow, ConsoleModifiers.None, '\0'), CursorDirection.Left);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapLAndRightArrow_ToMoveCursorRight()
    {
        AssertMovesCursor(new KeyChord(ConsoleKey.L, ConsoleModifiers.None, 'l'), CursorDirection.Right);
        AssertMovesCursor(new KeyChord(ConsoleKey.RightArrow, ConsoleModifiers.None, '\0'), CursorDirection.Right);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapG_ToMoveToTop()
    {
        AssertMovesToEdge(new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g'), EdgeTarget.Top);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapShiftG_ToMoveToBottom()
    {
        AssertMovesToEdge(new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G'), EdgeTarget.Bottom);
    }

    private static void AssertMovesCursor(KeyChord chord, CursorDirection expectedDirection)
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(chord, out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(expectedDirection, Assert.IsType<TuiMessage.MoveCursor>(message).Direction);
    }

    private static void AssertMovesToEdge(KeyChord chord, EdgeTarget expectedEdge)
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(chord, out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(expectedEdge, Assert.IsType<TuiMessage.MoveToEdge>(message).Edge);
    }

    [Theory]
    [InlineData(ConsoleKey.Q, ConsoleModifiers.None, 'q')]
    [InlineData(ConsoleKey.C, ConsoleModifiers.Control, '\u0003')]
    public void CreateDefaultGlobal_Should_MapQuitKeys_ToQuitRequested(
        ConsoleKey key,
        ConsoleModifiers modifiers,
        char keyChar)
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(new KeyChord(key, modifiers, keyChar), out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.QuitRequested>(message);
    }

    [Theory]
    [InlineData(ConsoleKey.N, ConsoleModifiers.Control, '\u000e', 1)]
    [InlineData(ConsoleKey.P, ConsoleModifiers.Control, '\u0010', -1)]
    public void CreateDefaultGlobal_Should_MapCycleViewKeys_ToCycleView(
        ConsoleKey key,
        ConsoleModifiers modifiers,
        char keyChar,
        int expectedDelta)
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(new KeyChord(key, modifiers, keyChar), out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(expectedDelta, Assert.IsType<TuiMessage.CycleView>(message).Delta);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapEnter_ToOpenSelected()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.OpenSelected>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapR_ToRefreshRequested()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.RefreshRequested>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapY_ToCopySelectedId()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.CopySelectedId>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapZ_ToToggleMaximize()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.Z, ConsoleModifiers.None, 'z'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ToggleMaximize>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapS_ToStatusPickerRequested()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.S, ConsoleModifiers.None, 's'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.StatusPickerRequested>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapP_ToPriorityPickerRequested()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.PriorityPickerRequested>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_ReturnFalse_When_KeyIsUnbound()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.F1, ConsoleModifiers.None, '\0'),
            out var message);

        // assert
        Assert.False(resolved);
        Assert.Null(message);
    }
}
