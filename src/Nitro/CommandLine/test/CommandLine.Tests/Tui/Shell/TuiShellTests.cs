using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

public sealed class TuiShellTests
{
    private static ConsoleKeyInfo KeyInfo(char keyChar, ConsoleKey key, ConsoleModifiers modifiers = ConsoleModifiers.None) =>
        new(
            keyChar,
            key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));

    private static TuiShell CreateShell(FakeTuiMode mode, int width = 80, int height = 24) =>
        new(new KeyDispatcher(KeyMap.CreateDefaultGlobal()), mode, width, height);

    private static string RenderToText(TuiShell shell)
    {
        var console = new TestConsole().Width(80);
        console.Write(shell.Render());
        return console.Output;
    }

    [Fact]
    public void Constructor_Should_CallOnEnter_OnActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();

        // act
        CreateShell(mode);

        // assert
        Assert.True(mode.EnterCalled);
    }

    [Fact]
    public void Handle_Should_OpenConfirmDialog_When_QuitRequested()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // assert
        Assert.True(dirty);
        Assert.Contains("Quit?", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_RaiseQuitConfirmed_When_YPressedWhileConfirmActive()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('y', ConsoleKey.Y)));

        // assert
        Assert.True(dirty);
        Assert.True(confirmed);
    }

    [Fact]
    public void Handle_Should_CloseConfirmDialogWithoutQuitting_When_NPressedWhileConfirmActive()
    {
        // arrange
        var mode = new FakeTuiMode { RenderText = "board" };
        var shell = CreateShell(mode);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('n', ConsoleKey.N)));

        // assert
        Assert.True(dirty);
        Assert.False(confirmed);
        var text = RenderToText(shell);
        Assert.DoesNotContain("Quit?", text);
        Assert.Contains("board", text);
    }

    [Fact]
    public void Handle_Should_PrioritizeConfirmDialogKeys_OverGlobalCopyBinding_When_ConfirmActive()
    {
        // The global table binds 'y' to CopySelectedId; while the confirm dialog is
        // active it must win that binding instead of falling through to global.
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);
        var confirmed = false;
        shell.QuitConfirmed += () => confirmed = true;
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('q', ConsoleKey.Q)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('y', ConsoleKey.Y)));

        // assert
        Assert.True(confirmed);
        Assert.DoesNotContain(mode.HandledMessages, m => m is TuiMessage.CopySelectedId);
    }

    [Fact]
    public void Handle_Should_ForwardRefreshRequested_ToActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains(mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Handle_Should_ForwardDataChangedEvent_ToActiveModeAsRefreshRequested()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);

        // act
        var dirty = shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.True(dirty);
        Assert.Contains(mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Handle_Should_DelegateUnhandledMessage_ToActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('j', ConsoleKey.J)));

        // assert
        var moveCursor = Assert.IsType<TuiMessage.MoveCursor>(Assert.Single(mode.HandledMessages));
        Assert.Equal(CursorDirection.Down, moveCursor.Direction);
    }

    [Fact]
    public void Handle_Should_DispatchFollowUpMessages_FromActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode
        {
            HandleResult = _ => [new TuiMessage.ShowToast("saved", ToastStyle.Success)]
        };
        var shell = CreateShell(mode);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains("saved", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ExpireToast_When_TickIsFarEnoughInTheFuture()
    {
        // arrange
        var mode = new FakeTuiMode
        {
            HandleResult = _ => [new TuiMessage.ShowToast("saved", ToastStyle.Success)]
        };
        var shell = CreateShell(mode);
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));
        Assert.Contains("saved", RenderToText(shell));

        // act
        var dirty = shell.Handle(new TuiEvent.TickEvent(DateTimeOffset.UtcNow.AddSeconds(4)));

        // assert
        Assert.True(dirty);
        Assert.DoesNotContain("saved", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_ReturnFalse_When_TickWithNoActiveToast()
    {
        // arrange
        var shell = CreateShell(new FakeTuiMode());

        // act
        var dirty = shell.Handle(new TuiEvent.TickEvent(DateTimeOffset.UtcNow));

        // assert
        Assert.False(dirty);
    }

    [Fact]
    public void Handle_Should_ResizeActiveMode_ReservingStatusRow_When_ResizeEvent()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode, width: 80, height: 24);

        // act
        var dirty = shell.Handle(new TuiEvent.ResizeEvent(100, 30));

        // assert
        Assert.True(dirty);
        Assert.Equal((100, 29), Assert.Single(mode.ResizeCalls));
    }

    [Fact]
    public void Render_Should_PassContentHeight_ReservingStatusRow_ToActiveMode()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = CreateShell(mode, width: 80, height: 24);

        // act
        shell.Render();

        // assert
        Assert.Equal((80, 23), Assert.Single(mode.RenderCalls));
    }
}
