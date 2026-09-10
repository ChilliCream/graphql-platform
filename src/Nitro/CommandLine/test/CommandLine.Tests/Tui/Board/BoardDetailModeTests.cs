using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardDetailModeTests
{
    private static string RenderToText(BoardDetailMode mode, int width = 80, int height = 24)
    {
        var console = new TestConsole().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    [Fact]
    public void SelectedTaskId_Should_BeNull_BeforeAnyTaskOpened()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = new BoardDetailMode(store);

        // assert
        Assert.Null(((ITuiMode)mode).SelectedTaskId);
    }

    [Fact]
    public void OpenOnTask_Should_LoadTaskThroughStore_And_ExposeItAsSelected()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1"));
        var mode = new BoardDetailMode(store);

        // act
        mode.OpenOnTask("a-1");

        // assert
        Assert.Equal("a-1", ((ITuiMode)mode).SelectedTaskId);
        Assert.Contains("a-1", RenderToText(mode));
    }

    [Fact]
    public void OpenOnTask_Should_RenderNotFoundMessage_When_TaskDoesNotExist()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = new BoardDetailMode(store);

        // act
        mode.OpenOnTask("missing");

        // assert
        Assert.Contains("not found", RenderToText(mode));
    }

    [Fact]
    public void OpenOnTask_Should_ReplaceWhicheverTaskWasPreviouslyLoaded()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1"));
        store.Tasks.Add(TaskItemBuilder.Create("a-2"));
        var mode = new BoardDetailMode(store);
        mode.OpenOnTask("a-1");

        // act
        mode.OpenOnTask("a-2");

        // assert
        Assert.Equal("a-2", ((ITuiMode)mode).SelectedTaskId);
    }

    [Fact]
    public void Handle_RefreshRequested_Should_ReloadCurrentTaskThroughStore()
    {
        // arrange: the label is added to the store only after the initial
        // load, so it only shows up once RefreshRequested re-fetches.
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1"));
        var mode = new BoardDetailMode(store);
        mode.OpenOnTask("a-1");
        Assert.DoesNotContain("urgent", RenderToText(mode));
        store.Labels["a-1"] = ["urgent"];

        // act
        var messages = mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Empty(messages);
        Assert.Contains("urgent", RenderToText(mode));
    }

    [Fact]
    public void Handle_RefreshRequested_Should_BeNoOp_When_NoTaskOpened()
    {
        // arrange
        var store = new FakeTaskStore();
        var mode = new BoardDetailMode(store);

        // act
        var exception = Record.Exception(() => mode.Handle(new TuiMessage.RefreshRequested()));

        // assert
        Assert.Null(exception);
        Assert.Null(((ITuiMode)mode).SelectedTaskId);
    }

    [Fact]
    public void Handle_MoveCursorDown_Should_NotThrow_When_TaskOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveCursor(CursorDirection.Down));

    [Fact]
    public void Handle_MoveCursorUp_Should_NotThrow_When_TaskOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveCursor(CursorDirection.Up));

    [Fact]
    public void Handle_MoveToEdgeTop_Should_NotThrow_When_TaskOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveToEdge(EdgeTarget.Top));

    [Fact]
    public void Handle_MoveToEdgeBottom_Should_NotThrow_When_TaskOpened()
        => AssertHandleDoesNotThrow(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

    private static void AssertHandleDoesNotThrow(TuiMessage message)
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1"));
        var mode = new BoardDetailMode(store);
        mode.OpenOnTask("a-1");
        RenderToText(mode, height: 5);

        // act
        var exception = Record.Exception(() => mode.Handle(message));

        // assert
        Assert.Null(exception);
    }
}
