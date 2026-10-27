using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

public sealed class MemoryKeyMapTests
{
    [Fact]
    public void CreateDefault_Should_ReturnMoveCursorDown_When_JOrDownArrowIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var j = new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j');
        var down = new KeyChord(ConsoleKey.DownArrow, ConsoleModifiers.None, '\0');

        // act
        keyMap.TryResolve(j, out var jMessage);
        keyMap.TryResolve(down, out var downMessage);

        // assert
        Assert.Equal(CursorDirection.Down, Assert.IsType<TuiMessage.MoveCursor>(jMessage).Direction);
        Assert.Equal(CursorDirection.Down, Assert.IsType<TuiMessage.MoveCursor>(downMessage).Direction);
    }

    [Fact]
    public void CreateDefault_Should_ReturnMoveCursorUp_When_KOrUpArrowIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var k = new KeyChord(ConsoleKey.K, ConsoleModifiers.None, 'k');
        var up = new KeyChord(ConsoleKey.UpArrow, ConsoleModifiers.None, '\0');

        // act
        keyMap.TryResolve(k, out var kMessage);
        keyMap.TryResolve(up, out var upMessage);

        // assert
        Assert.Equal(CursorDirection.Up, Assert.IsType<TuiMessage.MoveCursor>(kMessage).Direction);
        Assert.Equal(CursorDirection.Up, Assert.IsType<TuiMessage.MoveCursor>(upMessage).Direction);
    }

    [Fact]
    public void CreateDefault_Should_ReturnMoveToEdge_When_GOrShiftGIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var g = new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g');
        var shiftG = new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G');

        // act
        keyMap.TryResolve(g, out var top);
        keyMap.TryResolve(shiftG, out var bottom);

        // assert
        Assert.Equal(EdgeTarget.Top, Assert.IsType<TuiMessage.MoveToEdge>(top).Edge);
        Assert.Equal(EdgeTarget.Bottom, Assert.IsType<TuiMessage.MoveToEdge>(bottom).Edge);
    }

    [Fact]
    public void CreateDefault_Should_ReturnOpenSelected_When_EnterIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var enter = new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r');

        // act
        var resolved = keyMap.TryResolve(enter, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.OpenSelected>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnCycleView_When_FIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var f = new KeyChord(ConsoleKey.F, ConsoleModifiers.None, 'f');

        // act
        var resolved = keyMap.TryResolve(f, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.CycleView>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnSearchRequested_When_SlashIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var slash = new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/');

        // act
        var resolved = keyMap.TryResolve(slash, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.SearchRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnCopySelectedId_When_YIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var y = new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y');

        // act
        var resolved = keyMap.TryResolve(y, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.CopySelectedId>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnRefreshRequested_When_RIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var r = new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r');

        // act
        var resolved = keyMap.TryResolve(r, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.RefreshRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnQuitRequested_When_QOrCtrlCIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var q = new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q');
        var ctrlC = new KeyChord(ConsoleKey.C, ConsoleModifiers.Control, '\u0003');

        // act
        keyMap.TryResolve(q, out var qMessage);
        keyMap.TryResolve(ctrlC, out var ctrlCMessage);

        // assert
        Assert.IsType<TuiMessage.QuitRequested>(qMessage);
        Assert.IsType<TuiMessage.QuitRequested>(ctrlCMessage);
    }

    [Fact]
    public void CreateDefault_Should_ReturnBack_When_EscapeIsResolved()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var escape = new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, '\u001b');

        // act
        var resolved = keyMap.TryResolve(escape, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.Back>(message);
    }

    [Fact]
    public void CreateDefault_Should_NotResolvePromoteOrForget_When_TheyMatchTheRetiredSplitPaneBoard()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var p = new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p');
        var d = new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd');

        // act
        var pResolved = keyMap.TryResolve(p, out _);
        var dResolved = keyMap.TryResolve(d, out _);

        // assert
        Assert.False(pResolved);
        Assert.False(dResolved);
    }

    [Fact]
    public void CreateDefault_Should_ListHintsInBindingOrder_When_KeyMapIsBuilt()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();

        // act
        var keys = keyMap.Hints.Select(h => h.Key).ToList();

        // assert
        Assert.Equal(["hjkl", "enter", "f", "/", "y", "r", "esc", "q"], keys);
    }
}
