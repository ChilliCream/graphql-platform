using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Memory;

public sealed class MemoryKeyMapTests
{
    [Fact]
    public void CreateDefault_Should_MapJAndDownArrow_ToMoveCursorDown()
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
    public void CreateDefault_Should_MapF_ToCycleView()
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
    public void CreateDefault_Should_MapS_ToCycleScopeRequested()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var s = new KeyChord(ConsoleKey.S, ConsoleModifiers.None, 's');

        // act
        var resolved = keyMap.TryResolve(s, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.CycleScopeRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapSlash_ToSearchRequested()
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
    public void CreateDefault_Should_MapP_ToPromoteRequested()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var p = new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p');

        // act
        var resolved = keyMap.TryResolve(p, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.PromoteRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapD_ToForgetRequested()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var d = new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd');

        // act
        var resolved = keyMap.TryResolve(d, out var message);

        // assert
        Assert.True(resolved);
        Assert.IsType<TuiMessage.ForgetRequested>(message);
    }

    [Fact]
    public void CreateDefault_Should_MapQAndCtrlC_ToQuitRequested()
    {
        // arrange
        var keyMap = MemoryKeyMap.CreateDefault();
        var q = new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q');
        var ctrlC = new KeyChord(ConsoleKey.C, ConsoleModifiers.Control, '');

        // act
        keyMap.TryResolve(q, out var qMessage);
        keyMap.TryResolve(ctrlC, out var ctrlCMessage);

        // assert
        Assert.IsType<TuiMessage.QuitRequested>(qMessage);
        Assert.IsType<TuiMessage.QuitRequested>(ctrlCMessage);
    }
}
