using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed class TaskDetailBodyTests
{
    [Fact]
    public async Task Build_Should_ReturnEmpty_When_NoTaskIsLoaded()
    {
        // arrange
        var model = new TaskDetailModel(new FakeTaskStore());
        await model.LoadAsync("missing", CancellationToken.None);

        // act
        var lines = TaskDetailBody.Build(model, 40, focused: true);

        // assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task Build_Should_OmitEmptySections_And_KeepOrder()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create(
            "t-1",
            description: "desc",
            notes: "note");
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var lines = TaskDetailBody.Build(model, 40, focused: true).Select(l => l.Content).ToList();

        // assert: Design, Acceptance criteria, Dependencies, Blocks, Comments are all
        // empty and must not appear; a single blank line separates the two present
        // sections.
        Assert.Equal(["Description:", "desc", "", "Notes:", "note"], lines);
    }

    [Fact]
    public async Task Build_Should_IncludeAllSections_InOrder_When_AllArePresent()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create(
            "t-1",
            description: "d",
            design: "de",
            acceptanceCriteria: "ac",
            notes: "n");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Blocks, DependsOnId = "t-2", Status = TaskStates.Open, Title = "Dep" }
        ];
        store.Dependents["t-1"] =
        [
            new TaskDependentDetail { Type = TaskDependencyTypes.Related, TaskId = "t-3", Status = TaskStates.Open, Title = "Blocked" }
        ];
        store.Comments["t-1"] =
        [
            new TaskComment { TaskId = "t-1", Author = "alice", Text = "c", CreatedAt = DateTimeOffset.UnixEpoch }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var headers = TaskDetailBody.Build(model, 40, focused: true)
            .Select(l => l.Content)
            .Where(c => c.EndsWith(':'))
            .ToList();

        // assert
        Assert.Equal(
            ["Description:", "Design:", "Acceptance criteria:", "Notes:", "Dependencies:", "Blocks:", "Comments:"],
            headers);
    }

    [Fact]
    public async Task Build_Should_MarkDependencyAndBlocksRows_AsMarkup()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Blocks, DependsOnId = "t-2", Status = TaskStates.Open, Title = "Dep" }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var lines = TaskDetailBody.Build(model, 40, focused: true);

        // assert: the "Dependencies:" header line is plain, the row beneath is markup
        var headerLine = lines.Single(l => l.Content == "Dependencies:");
        var rowLine = lines[lines.ToList().IndexOf(headerLine) + 1];
        Assert.False(headerLine.IsMarkup);
        Assert.True(rowLine.IsMarkup);
    }
}
