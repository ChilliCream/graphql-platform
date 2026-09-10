using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BoardView TwoColumnView() => new()
    {
        Name = "Test",
        Columns =
        [
            new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] },
            new ColumnDefinition { Name = "Closed", Statuses = [TaskStates.Closed] }
        ]
    };

    [Fact]
    public async Task RefreshAsync_Should_LoadEveryColumn()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Closed));
        var state = new BoardState(TwoColumnView(), new BoardDataLoader(store, new FakeTimeProvider(Now)));

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], state.Columns[0].Tasks.Select(t => t.Id));
        Assert.Equal(["a-2"], state.Columns[1].Tasks.Select(t => t.Id));
    }

    [Fact]
    public async Task RefreshAsync_Should_KeepSelectedRowOnSameTask_When_TaskStillPresentAfterReorder()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open, createdAt: Now));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: Now.AddDays(1)));
        var state = new BoardState(TwoColumnView(), new BoardDataLoader(store, new FakeTimeProvider(Now)));
        await state.RefreshAsync(CancellationToken.None);
        state.Columns[0].SelectedRow = 1; // a-2, the later-created task

        // act: a new, earlier-priority task pushes a-2 to a different row on refresh
        store.Tasks.Add(TaskItemBuilder.Create(
            "a-0", status: TaskStates.Open, priority: TaskPriorities.Critical, createdAt: Now.AddDays(-1)));
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["a-0", "a-1", "a-2"], state.Columns[0].Tasks.Select(t => t.Id));
        Assert.Equal(2, state.Columns[0].SelectedRow);
        Assert.Equal("a-2", state.Columns[0].SelectedTaskId);
    }

    [Fact]
    public async Task RefreshAsync_Should_ClampSelectedRow_When_SelectedTaskNoLongerPresent()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        store.Tasks.Add(TaskItemBuilder.Create("a-2", status: TaskStates.Open, createdAt: Now.AddDays(1)));
        var state = new BoardState(TwoColumnView(), new BoardDataLoader(store, new FakeTimeProvider(Now)));
        await state.RefreshAsync(CancellationToken.None);
        state.Columns[0].SelectedRow = 1; // a-2

        // act: a-2 closes and drops out of the Open column
        store.Tasks.RemoveAt(1);
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["a-1"], state.Columns[0].Tasks.Select(t => t.Id));
        Assert.Equal(0, state.Columns[0].SelectedRow);
    }

    [Fact]
    public async Task RefreshAsync_Should_KeepSelectedRowAtZero_When_ColumnBecomesEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a-1", status: TaskStates.Open));
        var state = new BoardState(TwoColumnView(), new BoardDataLoader(store, new FakeTimeProvider(Now)));
        await state.RefreshAsync(CancellationToken.None);

        // act
        store.Tasks.Clear();
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Empty(state.Columns[0].Tasks);
        Assert.Equal(0, state.Columns[0].SelectedRow);
        Assert.Null(state.Columns[0].SelectedTaskId);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 1)]
    public void FocusColumn_Should_ClampToValidColumnRange(int requested, int expected)
    {
        // arrange
        var store = new FakeTaskStore();
        var state = new BoardState(TwoColumnView(), new BoardDataLoader(store, new FakeTimeProvider(Now)));

        // act
        state.FocusColumn(requested);

        // assert
        Assert.Equal(expected, state.FocusedColumnIndex);
    }
}
