using System.Text.RegularExpressions;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed partial class TaskDetailSidebarTests
{
    [Fact]
    public void Build_Should_OmitOptionalFields_When_NotSet()
    {
        // arrange
        var task = TaskItemBuilder.Create("t-1", createdBy: "alice");

        // act
        var lines = TaskDetailSidebar.Build(task, [], []).Select(StripMarkupTags);

        // assert: only the always-present status/priority/type/created/updated
        // lines remain, none of the optional fields.
        Assert.Equal(
            [
                "○ open",
                "Priority: P2",
                "Type: task",
                $"Created: {TaskDates.Format(task.CreatedAt)} by alice",
                $"Updated: {TaskDates.Format(task.UpdatedAt)}"
            ],
            lines);
    }

    [Fact]
    public void Build_Should_IncludeAllOptionalFields_When_Set()
    {
        // arrange
        var task = TaskItemBuilder.Create(
            "t-1",
            status: TaskStates.Closed,
            assignee: "bob",
            estimatedMinutes: 30,
            dueAt: DateTimeOffset.UnixEpoch,
            deferUntil: DateTimeOffset.UnixEpoch,
            createdBy: "alice",
            closedAt: DateTimeOffset.UnixEpoch,
            closeReason: "done");

        // act
        var lines = TaskDetailSidebar.Build(task, ["backend"], ["t-0:not closed"]).Select(StripMarkupTags);

        // assert
        Assert.Equal(
            [
                "✓ closed",
                "Priority: P2",
                "Type: task",
                "Assignee: bob",
                $"Due: {TaskDates.Format(task.DueAt!.Value)}",
                $"Deferred until: {TaskDates.Format(task.DeferUntil!.Value)}",
                "Estimate: 30m",
                $"Created: {TaskDates.Format(task.CreatedAt)} by alice",
                $"Updated: {TaskDates.Format(task.UpdatedAt)}",
                $"Closed: {TaskDates.Format(task.ClosedAt!.Value)} (done)",
                "Labels: backend",
                "Blocked by: t-0:not closed"
            ],
            lines);
    }

    [Fact]
    public void Build_Should_NotShowClosedLine_When_StatusIsNotClosed()
    {
        // arrange
        var task = TaskItemBuilder.Create("t-1", status: TaskStates.Open, closedAt: DateTimeOffset.UnixEpoch);

        // act
        var lines = TaskDetailSidebar.Build(task, [], []).Select(StripMarkupTags);

        // assert
        Assert.Equal(
            [
                "○ open",
                "Priority: P2",
                "Type: task",
                $"Created: {TaskDates.Format(task.CreatedAt)} by ",
                $"Updated: {TaskDates.Format(task.UpdatedAt)}"
            ],
            lines);
    }

    private static string StripMarkupTags(string line) => MarkupTagPattern().Replace(line, "");

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex MarkupTagPattern();
}
