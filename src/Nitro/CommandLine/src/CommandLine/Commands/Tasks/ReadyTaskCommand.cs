using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ReadyTaskCommand : Command
{
    public ReadyTaskCommand() : base("ready")
    {
        Description = "List tasks that are ready to work on.";

        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskAssigneeOption>.Instance);
        Options.Add(Opt<TaskLabelOption>.Instance);
        Options.Add(Opt<TaskLimitOption>.Instance);
        Options.Add(Opt<TaskIncludeDeferredOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("task ready", "task ready --assignee alice --limit 5");

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
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var priorityValue = parseResult.GetValue(Opt<TaskPriorityOption>.Instance);
        var assignee = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance);
        var labels = parseResult.GetValue(Opt<TaskLabelOption>.Instance);
        var limit = parseResult.GetValue(Opt<TaskLimitOption>.Instance);
        var includeDeferred = parseResult.GetValue(Opt<TaskIncludeDeferredOption>.Instance);

        var unassigned = !string.IsNullOrEmpty(assignee)
            && string.Equals(assignee, "unassigned", StringComparison.OrdinalIgnoreCase);

        var priorityRange = priorityValue is null
            ? default((int Min, int Max)?)
            : TaskPriorities.ParseRange(priorityValue);

        var filter = new TaskFilter
        {
            Statuses = [TaskStates.Open],
            PriorityMin = priorityRange?.Min,
            PriorityMax = priorityRange?.Max,
            Unassigned = unassigned,
            Assignee = unassigned ? null : assignee,
            Labels = labels,
            DeferredVisibleAt = includeDeferred ? null : timeProvider.GetUtcNow(),
            ExcludeBlocked = true,
            Limit = limit,
            Ordering = TaskOrdering.ReadyPick
        };

        var readyTasks = await store.QueryTasksAsync(filter, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<TaskSummaryResult>(readyTasks.Select(ToSummary).ToArray()));
            return ExitCodes.Success;
        }

        if (readyTasks.Count == 0)
        {
            console.WriteLine("No ready tasks.");
            return ExitCodes.Success;
        }

        foreach (var task in readyTasks)
        {
            console.WriteLine(FormatRow(task));
        }

        console.WriteLine();
        console.WriteLine($"{readyTasks.Count} task(s)");

        return ExitCodes.Success;
    }

    private static string FormatRow(TaskItem task)
        => $"{task.Id}  {TaskPriorities.Format(task.Priority)}  {task.Type}  "
            + $"{task.Status}  {task.Title}";

    private static TaskSummaryResult ToSummary(TaskItem task)
        => new(task.Id, task.Priority, task.Type, task.Status, task.Title);
}
