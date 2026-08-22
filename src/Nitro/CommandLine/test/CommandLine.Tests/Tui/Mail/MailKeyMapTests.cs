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
    public void CreateDefault_Should_MapU_ToToggleReadRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var u = new KeyChord(ConsoleKey.U, ConsoleModifiers.None, 'u');

        // act
        var resolved = keyMap.TryResolve(u, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ToggleReadRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapA_ToArchiveRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var a = new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a');

        // act
        var resolved = keyMap.TryResolve(a, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ArchiveRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapR_ToReplyRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var r = new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r');

        // act
        var resolved = keyMap.TryResolve(r, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ReplyRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapC_ToComposeRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var c = new KeyChord(ConsoleKey.C, ConsoleModifiers.None, 'c');

        // act
        var resolved = keyMap.TryResolve(c, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ComposeRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapShiftR_ToRefreshRequested()
    {
        // arrange: refresh moves off the bare r chord to make room for reply.
        var keyMap = MailKeyMap.CreateDefault();
        var shiftR = new KeyChord(ConsoleKey.R, ConsoleModifiers.Shift, 'R');

        // act
        var resolved = keyMap.TryResolve(shiftR, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.RefreshRequested>(message);
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
    public void CreateDefault_Should_MapShiftI_ToSelectInboxRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var shiftI = new KeyChord(ConsoleKey.I, ConsoleModifiers.Shift, 'I');

        // act
        var resolved = keyMap.TryResolve(shiftI, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.SelectInboxRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapShiftS_ToSelectSentRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var shiftS = new KeyChord(ConsoleKey.S, ConsoleModifiers.Shift, 'S');

        // act
        var resolved = keyMap.TryResolve(shiftS, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.SelectSentRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapShiftL_ToSelectAllMailRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var shiftL = new KeyChord(ConsoleKey.L, ConsoleModifiers.Shift, 'L');

        // act
        var resolved = keyMap.TryResolve(shiftL, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.SelectAllMailRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapShiftW_ToSelectWorkspaceMailRequested()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var shiftW = new KeyChord(ConsoleKey.W, ConsoleModifiers.Shift, 'W');

        // act
        var resolved = keyMap.TryResolve(shiftW, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.SelectWorkspaceMailRequested>(message);
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
