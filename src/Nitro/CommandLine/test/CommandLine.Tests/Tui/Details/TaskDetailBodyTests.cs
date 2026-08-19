using System.Text.RegularExpressions;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed partial class TaskDetailBodyTests
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
        var lines = TaskDetailBody.Build(model, 40, focused: true)
            .Select(l => StripMarkupTags(l.Content))
            .ToList();

        // assert: Design, Acceptance criteria, Dependencies, Blocks, Comments are all
        // empty and must not appear; each header is followed by a blank line, and a
        // second blank line separates the two present sections.
        Assert.Equal(["Description", "", "desc", "", "Notes", "", "note"], lines);
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

        var knownHeaders = new HashSet<string>(
            ["Description", "Design", "Acceptance criteria", "Notes", "Dependencies", "Blocks", "Comments"]);

        // act
        var headers = TaskDetailBody.Build(model, 40, focused: true)
            .Select(l => StripMarkupTags(l.Content))
            .Where(knownHeaders.Contains)
            .ToList();

        // assert
        Assert.Equal(
            ["Description", "Design", "Acceptance criteria", "Notes", "Dependencies", "Blocks", "Comments"],
            headers);
    }

    [Fact]
    public async Task Build_Should_MarkSectionHeaders_AsMarkup()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1", description: "desc");
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var lines = TaskDetailBody.Build(model, 40, focused: true);

        // assert: the header is styled markup, followed by a blank separator, then
        // the plain-text content.
        Assert.Equal("Description", StripMarkupTags(lines[0].Content));
        Assert.True(lines[0].IsMarkup);
        Assert.Equal(new TaskDetailBodyLine(string.Empty, false), lines[1]);
        Assert.Equal(new TaskDetailBodyLine("desc", false), lines[2]);
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
        var lines = TaskDetailBody.Build(model, 40, focused: true).ToList();

        // assert: the styled "Dependencies" header line is followed by a blank
        // separator, then the markup row.
        var headerIndex = lines.FindIndex(l => StripMarkupTags(l.Content) == "Dependencies");
        var blankLine = lines[headerIndex + 1];
        var rowLine = lines[headerIndex + 2];

        Assert.True(lines[headerIndex].IsMarkup);
        Assert.Equal(new TaskDetailBodyLine(string.Empty, false), blankLine);
        Assert.True(rowLine.IsMarkup);
    }

    [Fact]
    public async Task Build_Should_MarkSelectedRow_AsSelected_When_Focused()
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
        var focusedLines = TaskDetailBody.Build(model, 40, focused: true);
        var unfocusedLines = TaskDetailBody.Build(model, 40, focused: false);

        // assert: the sole dependency row is selected only while focused.
        Assert.Contains(focusedLines, l => l.IsSelectedRow);
        Assert.DoesNotContain(unfocusedLines, l => l.IsSelectedRow);
    }

    private static string StripMarkupTags(string line) => MarkupTagPattern().Replace(line, "");

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex MarkupTagPattern();
}
