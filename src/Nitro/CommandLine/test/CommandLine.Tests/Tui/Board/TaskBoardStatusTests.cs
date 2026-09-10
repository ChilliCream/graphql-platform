using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

/// <summary>
/// Pins <see cref="TaskBoardStatus.Resolve"/> against the same scenarios
/// <see cref="BoardDataLoaderTests"/> exercises through
/// <see cref="BoardDataLoader"/>'s Blocked and Deferred columns, so a future
/// change to the resolver cannot silently drift from what the Board shows.
/// </summary>
public sealed class TaskBoardStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_Should_ReturnBlocked_When_TaskHasAnUnmetBlockingDependency()
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Open);
        var blocked = new Dictionary<string, IReadOnlyList<string>> { ["a-1"] = ["a-9:open"] };

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.Blocked, status);
    }

    [Fact]
    public void Resolve_Should_ReturnBlocked_When_TaskStatusIsManuallyBlocked()
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Blocked);
        var blocked = new Dictionary<string, IReadOnlyList<string>>();

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.Blocked, status);
    }

    [Fact]
    public void Resolve_Should_ReturnDeferred_When_TaskStatusIsDeferred()
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Deferred);
        var blocked = new Dictionary<string, IReadOnlyList<string>>();

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.Deferred, status);
    }

    [Fact]
    public void Resolve_Should_ReturnDeferred_When_DeferUntilIsInTheFuture()
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Open, deferUntil: Now.AddDays(1));
        var blocked = new Dictionary<string, IReadOnlyList<string>>();

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.Deferred, status);
    }

    [Fact]
    public void Resolve_Should_ReturnReady_When_DeferUntilIsInThePast()
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Open, deferUntil: Now.AddDays(-1));
        var blocked = new Dictionary<string, IReadOnlyList<string>>();

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskBoardStatus.Ready, status);
    }

    [Fact]
    public void Resolve_Should_ReturnDeferred_When_TaskIsBothBlockedAndDeferred()
    {
        // arrange: a task waiting on both a dependency and a date lands in
        // one column, not two -- Deferred wins, matching BoardDataLoader.
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Open, deferUntil: Now.AddDays(1));
        var blocked = new Dictionary<string, IReadOnlyList<string>> { ["a-1"] = ["a-9:open"] };

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.Deferred, status);
    }

    [Fact]
    public void Resolve_Should_ReturnReady_When_OpenTaskHasNoBlockersOrDeferral()
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.Open);
        var blocked = new Dictionary<string, IReadOnlyList<string>>();

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskBoardStatus.Ready, status);
    }

    [Fact]
    public void Resolve_Should_ReturnInProgress_When_InProgressTaskHasAnUnmetDependency()
    {
        // arrange: the Board rule -- once work has started, in progress wins
        // over blocked.
        var task = TaskItemBuilder.Create("a-1", status: TaskStates.InProgress);
        var blocked = new Dictionary<string, IReadOnlyList<string>> { ["a-1"] = ["a-9:open"] };

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.InProgress, status);
    }

    [Theory]
    [InlineData(TaskStates.Closed)]
    [InlineData(TaskStates.Tombstone)]
    [InlineData(TaskStates.Archived)]
    public void Resolve_Should_ReturnClosed_When_TaskIsTerminal_EvenWithAnUnmetDependency(string terminalStatus)
    {
        // arrange
        var task = TaskItemBuilder.Create("a-1", status: terminalStatus);
        var blocked = new Dictionary<string, IReadOnlyList<string>> { ["a-1"] = ["a-9:open"] };

        // act
        var status = TaskBoardStatus.Resolve(task, blocked, Now);

        // assert
        Assert.Equal(TaskStates.Closed, status);
    }
}
