using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskCommand : Command
{
    public ListTaskCommand() : base("list")
    {
        Description = "List tasks.";

        Options.Add(Opt<TaskStatusFilterOption>.Instance);
        Options.Add(Opt<TaskTypeOption>.Instance);
        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskAssigneeOption>.Instance);
        Options.Add(Opt<TaskLabelOption>.Instance);
        Options.Add(Opt<TaskLimitOption>.Instance);
        Options.Add(Opt<TaskAllOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "task list",
            "task list --status open --status in_progress",
            "task list --assignee alice --priority p1");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var statuses = parseResult.GetValue(Opt<TaskStatusFilterOption>.Instance);
        var type = parseResult.GetValue(Opt<TaskTypeOption>.Instance);
        var priority = parseResult.GetValue(Opt<TaskPriorityOption>.Instance);
        var assignee = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance);
        var labels = parseResult.GetValue(Opt<TaskLabelOption>.Instance);
        var limit = parseResult.GetValue(Opt<TaskLimitOption>.Instance);
        var all = parseResult.GetValue(Opt<TaskAllOption>.Instance);

        var priorityRange = priority is null ? default((int Min, int Max)?) : TaskPriorities.ParseRange(priority);

        var filter = new TaskFilter
        {
            Statuses = statuses,
            IncludeAll = all,
            Type = type,
            PriorityMin = priorityRange?.Min,
            PriorityMax = priorityRange?.Max,
            Assignee = assignee,
            Labels = labels,
            Limit = limit
        };

        var tasks = await store.QueryTasksAsync(filter, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskSummaryResult>(tasks.Select(ToSummary).ToArray()));
            return ExitCodes.Success;
        }

        if (tasks.Count == 0)
        {
            console.WriteLine("No tasks found.");
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

    private static TaskSummaryResult ToSummary(TaskItem task)
        => new(task.Id, task.Priority, task.Type, task.Status, task.Title);
}
