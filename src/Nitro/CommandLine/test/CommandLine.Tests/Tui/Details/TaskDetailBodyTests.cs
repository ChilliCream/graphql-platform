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
        // empty and must not appear; each present section renders as a 3-line box
        // (top border, content, bottom border), with a blank line separating them.
        Assert.Equal(
            [
                BoxTop("Description", 40),
                BoxContent("desc", 40),
                BoxBottom(40),
                "",
                BoxTop("Notes", 40),
                BoxContent("note", 40),
                BoxBottom(40)
            ],
            lines);
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

        var listStyleHeaders = new HashSet<string>(["Dependencies", "Blocks", "Comments"]);

        // act: Description, Design, Acceptance criteria, and Notes render as boxes,
        // so their names are extracted from the box's top border; Dependencies,
        // Blocks, and Comments stay a plain header line.
        var headers = TaskDetailBody.Build(model, 40, focused: true)
            .Select(l => StripMarkupTags(l.Content))
            .Where(l => l.StartsWith("╭─", StringComparison.Ordinal) || listStyleHeaders.Contains(l))
            .Select(l => l.StartsWith("╭─", StringComparison.Ordinal) ? BoxTitle(l) : l)
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

        // assert: the box's top and bottom borders are styled markup carrying the
        // section name; the content row is plain, unescaped text.
        Assert.Equal(BoxTop("Description", 40), StripMarkupTags(lines[0].Content));
        Assert.True(lines[0].IsMarkup);
        Assert.Equal(new TaskDetailBodyLine(BoxContent("desc", 40), false), lines[1]);
        Assert.Equal(BoxBottom(40), StripMarkupTags(lines[2].Content));
        Assert.True(lines[2].IsMarkup);
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

    /// <summary>
    /// The unstyled top border line <see cref="TaskDetailSectionBox"/> renders
    /// for <paramref name="title"/> at <paramref name="width"/> columns.
    /// </summary>
    private static string BoxTop(string title, int width)
        => $"╭─{title}{new string('─', Math.Max(0, width - 3 - title.Length))}╮";

    /// <summary>
    /// The content row <see cref="TaskDetailSectionBox"/> renders for one
    /// already-wrapped <paramref name="line"/> at <paramref name="width"/>
    /// columns.
    /// </summary>
    private static string BoxContent(string line, int width) => $"│ {line.PadRight(width - 4)} │";

    /// <summary>
    /// The unstyled bottom border line <see cref="TaskDetailSectionBox"/>
    /// renders at <paramref name="width"/> columns.
    /// </summary>
    private static string BoxBottom(int width) => $"╰{new string('─', Math.Max(0, width - 2))}╯";

    /// <summary>
    /// Recovers a section box's title from its unstyled top border line.
    /// </summary>
    private static string BoxTitle(string topBorderLine) => topBorderLine[2..^1].TrimEnd('─');

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex MarkupTagPattern();
}
