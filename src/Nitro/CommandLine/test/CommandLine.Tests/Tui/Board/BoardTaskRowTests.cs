using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardTaskRowTests
{
    private static TaskItem CreateTask(
        string id = "hc-10-abc",
        string title = "Fix bug",
        string status = TaskStates.Open,
        int priority = TaskPriorities.Medium,
        string type = TaskTypes.Task) => TaskItemBuilder.Create(id, status, priority, type, title);

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // arrange
        var task = CreateTask();
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var line = BoardTaskRow.Render(task, selected: false, maxWidth: 0, widths);

        // assert
        Assert.Empty(line);
    }

    [Fact]
    public void Render_Should_ShowEveryColumn_When_MaxWidthFits()
    {
        // arrange
        const int maxWidth = 60;
        var task = CreateTask(id: "hc-10-abc", title: "Fix bug", priority: TaskPriorities.Critical, type: TaskTypes.Bug);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));

        // assert
        Assert.Equal("  ○ bug           P0    hc-10-abc     Fix bug", line.TrimEnd());
    }

    [Fact]
    public void Render_Should_DropPriorityColumn_When_WidthIsTooNarrowForAllColumns()
    {
        // arrange
        const int maxWidth = 30;
        var task = CreateTask(priority: TaskPriorities.Critical, type: TaskTypes.Bug);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));

        // assert
        Assert.Equal("  ○ bug         hc-10-abc", line.TrimEnd());
        Assert.True(line.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void Render_Should_DropTypeColumn_When_WidthIsTooNarrowForTypeAndPriority()
    {
        // arrange
        const int maxWidth = 20;
        var task = CreateTask(id: "hc-10-abc", priority: TaskPriorities.Critical, type: TaskTypes.Bug);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));

        // assert
        Assert.Equal("  ○ hc-10-abc     F…", line.TrimEnd());
        Assert.True(line.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void Render_Should_TruncateTitleWithEllipsis_When_TitleExceedsRemainingWidth()
    {
        // arrange
        const int maxWidth = 40;
        var task = CreateTask(id: "hc-10-abc", title: "A very long task title that will not fit");
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));

        // assert
        Assert.Contains("…", line);
        Assert.True(line.GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void RenderHeader_Should_AlignColumnsWithRow_When_WidthIsWide()
    {
        // arrange
        const int maxWidth = 60;
        var task = CreateTask(id: "hc-10-abc", title: "Fix bug", priority: TaskPriorities.Critical, type: TaskTypes.Bug);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var header = Markup.Remove(BoardTaskRow.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));
        var typeOffset = header.IndexOf("TYPE", StringComparison.Ordinal);
        var prioOffset = header.IndexOf("PRIO", StringComparison.Ordinal);
        var idOffset = header.IndexOf("ID", StringComparison.Ordinal);
        var titleOffset = header.IndexOf("TITLE", StringComparison.Ordinal);
        var columnsAtHeaderOffsets = (
            Type: line.Substring(typeOffset, widths.Type).TrimEnd(),
            Prio: line.Substring(prioOffset, widths.Priority).Trim(),
            Id: line.Substring(idOffset, widths.Id).TrimEnd(),
            Title: line.Substring(titleOffset, 7));

        // assert
        Assert.Equal((Type: "bug", Prio: "P0", Id: "hc-10-abc", Title: "Fix bug"), columnsAtHeaderOffsets);
    }

    [Fact]
    public void RenderHeader_Should_DropTheSameColumnsAsRow_When_WidthIsNarrow()
    {
        // arrange
        const int maxWidth = 30;
        var task = CreateTask(priority: TaskPriorities.Critical, type: TaskTypes.Bug);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var header = Markup.Remove(BoardTaskRow.RenderHeader(maxWidth, widths));
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));

        // assert
        Assert.Equal("    TYPE        ID", header.TrimEnd());
        Assert.Equal("  ○ bug         hc-10-abc", line.TrimEnd());
    }

    [Fact]
    public void RenderHeader_Should_DropTypeColumn_When_WidthIsTooNarrowForTypeAndPriority()
    {
        // arrange
        const int maxWidth = 20;
        var task = CreateTask(id: "hc-10-abc", priority: TaskPriorities.Critical, type: TaskTypes.Bug);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var header = Markup.Remove(BoardTaskRow.RenderHeader(maxWidth, widths));

        // assert
        Assert.Equal("    ID            T…", header.TrimEnd());
    }

    [Fact]
    public void ComputeWidths_Should_WidenIdColumn_When_LongestIdExceedsMinimum()
    {
        // arrange
        var rows = new[] { CreateTask(id: "hc-10-a"), CreateTask(id: "hc-10-a-very-long-id") };

        // act
        var widths = BoardTaskRow.ComputeWidths(rows);

        // assert
        Assert.Equal("hc-10-a-very-long-id".Length, widths.Id);
    }

    [Fact]
    public void ComputeWidths_Should_UseMinimumOfEight_When_TypeIsShorterThanMinimum()
    {
        // arrange
        var rows = new[] { CreateTask(type: TaskTypes.Bug) };

        // act
        var widths = BoardTaskRow.ComputeWidths(rows);

        // assert
        Assert.Equal(8, widths.Type);
    }

    [Fact]
    public void Render_Should_ShowFullTypeName_When_TypeIsQuestion()
    {
        // arrange
        const int maxWidth = 60;
        var task = CreateTask(type: TaskTypes.Question);
        var widths = BoardTaskRow.ComputeWidths([task]);

        // act
        var line = Markup.Remove(BoardTaskRow.Render(task, selected: false, maxWidth: maxWidth, widths));

        // assert
        Assert.Contains("question", line);
    }

    [Fact]
    public void ComputeWidths_Should_WidenTypeColumn_When_CustomTypeExceedsMinimum()
    {
        // arrange
        var rows = new[] { CreateTask(type: "documentation") };

        // act
        var widths = BoardTaskRow.ComputeWidths(rows);

        // assert
        Assert.Equal("documentation".Length, widths.Type);
    }
}
