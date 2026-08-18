using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class StaleTaskCommand : Command
{
    public StaleTaskCommand() : base("stale")
    {
        Description = "List open tasks that have not been updated recently.";

        Options.Add(Opt<TaskDaysOption>.Instance);

        this.AddExamples("task stale", "task stale --days 14");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var timeProvider = services.GetRequiredService<TimeProvider>();

        var days = parseResult.GetValue(Opt<TaskDaysOption>.Instance) ?? 30;
        var threshold = timeProvider.GetUtcNow() - TimeSpan.FromDays(days);

        var filter = new TaskFilter
        {
            Statuses = [TaskStates.Open, TaskStates.InProgress],
            UpdatedBefore = threshold,
            Ordering = TaskOrdering.UpdatedAtAscending
        };

        var tasks = await store.QueryTasksAsync(filter, cancellationToken);

        if (tasks.Count == 0)
        {
            console.WriteLine("No stale tasks.");
            return ExitCodes.Success;
        }

        foreach (var task in tasks)
        {
            console.WriteLine(FormatRow(task));
        }

        console.WriteLine();
        console.WriteLine($"{tasks.Count} task(s)");

        return ExitCodes.Success;
    }

    private static string FormatRow(TaskItem task)
        => new TaskListRow
        {
            Id = task.Id,
            Priority = task.Priority,
            Type = task.Type,
            Status = task.Status,
            Title = task.Title
        }.Format();
}
