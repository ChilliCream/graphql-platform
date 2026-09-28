using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailKeyMapTests
{
    [Fact]
    public void CreateDefault_Should_ReturnMoveCursorDown_When_JOrDownArrowIsResolved()
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
    public void CreateDefault_Should_ReturnMoveCursorUp_When_KOrUpArrowIsResolved()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
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
        var keyMap = MailKeyMap.CreateDefault();
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
        var keyMap = MailKeyMap.CreateDefault();
        var enter = new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r');

        // act
        var resolved = keyMap.TryResolve(enter, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.OpenSelected>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnAgentFilterPickerRequested_When_PIsResolved()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var p = new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p');

        // act
        var resolved = keyMap.TryResolve(p, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.AgentFilterPickerRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnSearchRequested_When_SlashIsResolved()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
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
        var keyMap = MailKeyMap.CreateDefault();
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
        var keyMap = MailKeyMap.CreateDefault();
        var r = new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r');

        // act
        var resolved = keyMap.TryResolve(r, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.RefreshRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_ReturnQuitRequested_When_QIsResolved()
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
    public void CreateDefault_Should_ReturnBack_When_EscapeIsResolved()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var escape = new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, '\u001b');

        // act
        var resolved = keyMap.TryResolve(escape, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.Back>(message);
    }

    [Fact]
    public void CreateDefault_Should_NotResolveWriteGestures_When_TheyMatchTheRetiredSplitPaneBoard()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var u = new KeyChord(ConsoleKey.U, ConsoleModifiers.None, 'u');
        var a = new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a');
        var c = new KeyChord(ConsoleKey.C, ConsoleModifiers.None, 'c');
        var shiftV = new KeyChord(ConsoleKey.V, ConsoleModifiers.Shift, 'V');
        var z = new KeyChord(ConsoleKey.Z, ConsoleModifiers.None, 'z');

        // act
        var uResolved = keyMap.TryResolve(u, out _);
        var aResolved = keyMap.TryResolve(a, out _);
        var cResolved = keyMap.TryResolve(c, out _);
        var vResolved = keyMap.TryResolve(shiftV, out _);
        var zResolved = keyMap.TryResolve(z, out _);

        // assert
        Assert.False(uResolved);
        Assert.False(aResolved);
        Assert.False(cResolved);
        Assert.False(vResolved);
        Assert.False(zResolved);
    }

    [Fact]
    public void CreateDefault_Should_ListHintsInBindingOrder_When_KeyMapIsBuilt()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();

        // act
        var keys = keyMap.Hints.Select(h => h.Key).ToList();

        // assert
        Assert.Equal(["hjkl", "enter", "p", "/", "y", "r", "esc", "q"], keys);
    }
}
