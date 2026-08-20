using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailKeyMapTests
{
    [Fact]
    public void CreateDefault_Should_MapJAndDownArrow_ToMoveCursorDown()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
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
    public void CreateDefault_Should_MapEnter_ToOpenSelected()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var enter = new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r');

        // act
        var resolved = keyMap.TryResolve(enter, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.OpenSelected>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapTab_ToMoveCursorRight()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var tab = new KeyChord(ConsoleKey.Tab, ConsoleModifiers.None, '\t');

        // act
        var resolved = keyMap.TryResolve(tab, out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(CursorDirection.Right, Assert.IsType<TuiMessage.MoveCursor>(message).Direction);
    }

    [Fact]
    public void CreateDefault_Should_MapF_ToCycleViewForward()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var f = new KeyChord(ConsoleKey.F, ConsoleModifiers.None, 'f');

        // act
        var resolved = keyMap.TryResolve(f, out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(1, Assert.IsType<TuiMessage.CycleView>(message).Delta);
    }

    [Fact]
    public void CreateDefault_Should_MapShiftF_ToCycleViewBackward()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var shiftF = new KeyChord(ConsoleKey.F, ConsoleModifiers.Shift, 'F');

        // act
        var resolved = keyMap.TryResolve(shiftF, out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(-1, Assert.IsType<TuiMessage.CycleView>(message).Delta);
    }

    [Fact]
    public void CreateDefault_Should_MapT_ToToggleMaximize()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var t = new KeyChord(ConsoleKey.T, ConsoleModifiers.None, 't');

        // act
        var resolved = keyMap.TryResolve(t, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ToggleMaximize>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapQ_ToQuitRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var q = new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q');

        // act
        var resolved = keyMap.TryResolve(q, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.QuitRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapEscape_ToBack()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var escape = new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, '');

        // act
        var resolved = keyMap.TryResolve(escape, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.Back>(message);
    }

    [Fact]
    public void CreateDefault_Should_NotBind_TaskOnlyGestures()
    {
        // arrange: the mail board has no edit, delete, or dependency-tree
        // gestures, unlike the task board's global key table.
        var keyMap = MailKeyMap.CreateDefault();
        var e = new KeyChord(ConsoleKey.E, ConsoleModifiers.None, 'e');
        var t = new KeyChord(ConsoleKey.X, ConsoleModifiers.Shift, 'X');

        // act
        var eResolved = keyMap.TryResolve(e, out _);
        var xResolved = keyMap.TryResolve(t, out _);

        // assert
        Assert.False(eResolved);
        Assert.False(xResolved);
    }
}
