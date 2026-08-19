using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed class TaskDetailViewTests
{
    [Fact]
    public void Render_Should_NotThrow_When_WidthIsZeroOrLess()
    {
        // arrange
        var view = new TaskDetailView(new TaskDetailModel(new FakeTaskStore()));
        var console = new TestConsole().Width(10);

        // act
        var exception = Record.Exception(() => console.Write(view.Render(0, 10, focused: true)));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task Render_Should_FillTheFrame_When_SideBySide()
    {
        // arrange: 110 columns is at or above the 100-column stacking threshold.
        var view = await CreateViewAsync();

        // act
        var text = RenderToText(view, width: 110, height: 12);

        // assert
        Assert.Equal(12, CountRows(text));
    }

    [Fact]
    public async Task Render_Should_FillTheFrame_When_Stacked()
    {
        // arrange: 80 columns is below the 100-column stacking threshold.
        var view = await CreateViewAsync();

        // act
        var text = RenderToText(view, width: 80, height: 16);

        // assert
        Assert.Equal(16, CountRows(text));
    }

    [Fact]
    public async Task Render_Should_ShowMoreBelowIndicator_When_BodyOverflowsHeight()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create(
            "t-1", notes: string.Join('\n', Enumerable.Range(1, 30).Select(i => $"line {i}")));
        var view = await CreateViewAsync(store, "t-1");

        // act
        var text = RenderToText(view, width: 110, height: 10);

        // assert
        Assert.Contains("more below", text);
    }

    [Fact]
    public async Task Render_Should_ShowMoreAboveIndicator_After_ScrollingDown()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create(
            "t-1", notes: string.Join('\n', Enumerable.Range(1, 30).Select(i => $"line {i}")));
        var view = await CreateViewAsync(store, "t-1");

        // act: an initial render establishes the viewport's window height and
        // total line count, which scrolling depends on.
        RenderToText(view, width: 110, height: 10);

        for (var i = 0; i < 10; i++)
        {
            view.ScrollDown();
        }

        var text = RenderToText(view, width: 110, height: 10);

        // assert
        Assert.Contains("more above", text);
    }

    [Fact]
    public async Task Render_Should_KeepSelectedRow_Visible_When_CursorMovesPastTheViewport()
    {
        // arrange: ten dependency rows in a body panel too short to show them all
        // at once.
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        store.Dependencies["t-1"] = Enumerable.Range(0, 10)
            .Select(i => new TaskDependencyDetail
            {
                Type = TaskDependencyTypes.Related,
                DependsOnId = $"d-{i}",
                Status = TaskStates.Open,
                Title = $"Dep {i}"
            })
            .ToList();
        var model = await LoadModelAsync(store, "t-1");
        var view = new TaskDetailView(model);

        // act: move the row cursor down to the last row.
        for (var i = 0; i < 9; i++)
        {
            model.MoveRowCursor(CursorDirection.Down);
        }

        var text = RenderToText(view, width: 110, height: 10, focused: true);

        // assert: the now-selected last row scrolled into view, and the
        // viewport reports rows hidden above it.
        Assert.Contains("d-9", text);
        Assert.Contains("more above", text);
    }

    [Fact]
    public async Task Render_Should_ShowIdAndTitle_InBodyHeader()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1", title: "Sample task", description: "desc");
        var view = await CreateViewAsync(store, "t-1");

        // act
        var text = RenderToText(view, width: 110, height: 10);

        // assert
        Assert.Contains("t-1", text);
        Assert.Contains("Sample task", text);
    }

    [Fact]
    public async Task Render_Should_ShowNoDetailsMessage_When_TaskHasNoSections()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        var view = await CreateViewAsync(store, "t-1");

        // act
        var text = RenderToText(view, width: 110, height: 10);

        // assert
        Assert.Contains("No details.", text);
    }

    [Fact]
    public async Task Render_Should_ShowNotFoundMessage_When_TaskIsMissing()
    {
        // arrange
        var model = new TaskDetailModel(new FakeTaskStore());
        await model.LoadAsync("missing", CancellationToken.None);
        var view = new TaskDetailView(model);

        // act
        var text = RenderToText(view, width: 110, height: 10);

        // assert
        Assert.Contains("not found", text);
    }

    private static async Task<TaskDetailView> CreateViewAsync(FakeTaskStore? store = null, string taskId = "t-1")
    {
        store ??= new FakeTaskStore();

        if (!store.Tasks.ContainsKey(taskId))
        {
            store.Tasks[taskId] = TaskItemBuilder.Create(taskId, description: "desc");
        }

        var model = await LoadModelAsync(store, taskId);
        return new TaskDetailView(model);
    }

    private static async Task<TaskDetailModel> LoadModelAsync(FakeTaskStore store, string taskId)
    {
        var model = new TaskDetailModel(store);
        await model.LoadAsync(taskId, CancellationToken.None);
        return model;
    }

    private static string RenderToText(TaskDetailView view, int width, int height, bool focused = true)
    {
        var console = new TestConsole().Width(width).Height(height);
        console.Write(view.Render(width, height, focused));
        return console.Output;
    }

    private static int CountRows(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
}
