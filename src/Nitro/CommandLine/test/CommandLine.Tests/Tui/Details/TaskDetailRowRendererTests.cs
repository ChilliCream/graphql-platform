using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed class TaskDetailRowRendererTests
{
    [Fact]
    public void Render_Should_FitDisplayWidth_When_FixedPrefixExceedsMaxWidth()
    {
        // arrange
        const int maxWidth = 1;
        var row = new TaskDetailRow(
            Index: 0,
            Kind: TaskDetailRowKind.Dependency,
            Type: TaskTypes.Task,
            TargetId: "t-1",
            Status: TaskStates.Open,
            Title: "Detail");

        // act
        var line = TaskDetailRowRenderer.Render(row, selected: false, maxWidth);

        // assert
        Assert.True(Markup.Remove(line).GetCellWidth() <= maxWidth);
        Assert.Equal("…", Markup.Remove(line));
    }

    [Fact]
    public void Render_Should_FitDisplayWidth_When_RowContainsCjkAndEmoji()
    {
        // arrange
        const int maxWidth = 30;
        var row = new TaskDetailRow(
            Index: 0,
            Kind: TaskDetailRowKind.Dependency,
            Type: TaskTypes.Task,
            TargetId: "t-漢😀",
            Status: TaskStates.Open,
            Title: "漢😀 detail title");

        // act
        var line = TaskDetailRowRenderer.Render(row, selected: false, maxWidth);

        // assert
        Assert.True(Markup.Remove(line).GetCellWidth() <= maxWidth);
    }
}
