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
    public void CreateDefaultGlobal_Should_MapC_ToCreateTaskRequested()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.None, 'c'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.CreateTaskRequested>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapShiftC_ToCreateEpicRequested()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.Shift, 'C'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.CreateEpicRequested>(message);
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

    [Fact]
    public void CreateDefaultGlobal_Should_MapOem2_ToFocusSearchRequested()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.FocusSearchRequested>(message);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_MapDivide_ToFocusSearchRequested()
    {
        // arrange: some terminals report ConsoleKey.Divide (not Oem2) for a typed '/'.
        var keyMap = KeyMap.CreateDefaultGlobal();

        // act
        var resolved = keyMap.TryResolve(
            new KeyChord(ConsoleKey.Divide, ConsoleModifiers.None, '/'),
            out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.FocusSearchRequested>(message);
    }

    [Fact]
    public void TryResolve_Should_FallBackToKeyChar_When_ConsoleKeyDiffersButCharAndModifiersMatch()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/');
        var keyMap = new KeyMap([new KeyBinding(chord, () => new TuiMessage.FocusSearchRequested())]);
        var otherKeyReportingSameChar = new KeyChord(ConsoleKey.Divide, ConsoleModifiers.None, '/');

        // act
        var resolved = keyMap.TryResolve(otherKeyReportingSameChar, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.FocusSearchRequested>(message);
    }

    [Fact]
    public void TryResolve_Should_NotFallBackToKeyChar_When_ModifiersDiffer()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/');
        var keyMap = new KeyMap([new KeyBinding(chord, () => new TuiMessage.FocusSearchRequested())]);
        var sameCharDifferentModifiers = new KeyChord(ConsoleKey.Divide, ConsoleModifiers.Shift, '/');

        // act
        var resolved = keyMap.TryResolve(sameCharDifferentModifiers, out var message);

        // assert
        Assert.False(resolved);
        Assert.Null(message);
    }

    [Fact]
    public void Hints_Should_BeEmpty_When_NoBindingCarriesAHint()
    {
        // arrange
        var keyMap = new KeyMap([new KeyBinding(new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a'), () => new TuiMessage.RefreshRequested())]);

        // assert
        Assert.Empty(keyMap.Hints);
    }

    [Fact]
    public void Hints_Should_OmitBindings_Without_AHint()
    {
        // arrange: only the second binding carries a hint.
        var keyMap = new KeyMap(
        [
            new KeyBinding(new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a'), () => new TuiMessage.RefreshRequested()),
            new KeyBinding(
                new KeyChord(ConsoleKey.B, ConsoleModifiers.None, 'b'),
                () => new TuiMessage.RefreshRequested(),
                new KeyHint("b", "refresh"))
        ]);

        // assert
        Assert.Equal([new KeyHint("b", "refresh")], keyMap.Hints);
    }

    [Fact]
    public void Hints_Should_PreserveConstructorOrder()
    {
        // arrange
        var keyMap = new KeyMap(
        [
            new KeyBinding(
                new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a'),
                () => new TuiMessage.RefreshRequested(),
                new KeyHint("a", "first")),
            new KeyBinding(
                new KeyChord(ConsoleKey.B, ConsoleModifiers.None, 'b'),
                () => new TuiMessage.RefreshRequested(),
                new KeyHint("b", "second"))
        ]);

        // assert
        Assert.Equal([new KeyHint("a", "first"), new KeyHint("b", "second")], keyMap.Hints);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_EndItsHints_With_Quit()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // assert
        Assert.NotEmpty(keyMap.Hints);
        Assert.Equal(new KeyHint("q", "quit"), keyMap.Hints[^1]);
    }

    [Fact]
    public void CreateDefaultGlobal_Should_Hint_TheFourDirectionKeys_AsOneMoveEntry()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();

        // assert: j/k/h/l and their arrow-key equivalents collapse into a
        // single "move" hint rather than one entry per key.
        Assert.Single(keyMap.Hints, h => h.Action == "move");
    }

    [Fact]
    public void TryResolve_Should_NotFallBackToKeyChar_When_CharIsControlChar()
    {
        // arrange: Ctrl+C reports KeyChar '\u0003', which is a control char and must not
        // fall back onto an unrelated binding that happens to share the char.
        var chord = new KeyChord(ConsoleKey.C, ConsoleModifiers.Control, '\u0003');
        var keyMap = new KeyMap([new KeyBinding(chord, () => new TuiMessage.QuitRequested())]);
        var differentKeySameControlChar = new KeyChord(ConsoleKey.D3, ConsoleModifiers.Control, '\u0003');

        // act
        var resolved = keyMap.TryResolve(differentKeySameControlChar, out var message);

        // assert
        Assert.False(resolved);
        Assert.Null(message);
    }
}
