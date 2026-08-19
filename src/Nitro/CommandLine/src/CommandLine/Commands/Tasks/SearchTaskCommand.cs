using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class SearchTaskCommand : Command
{
    public SearchTaskCommand() : base("search")
    {
        Description = "Search tasks by text.";

        Arguments.Add(Opt<SearchTextArgument>.Instance);
        Options.Add(Opt<TaskLimitOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks search \"parser\"", "agent tasks search \"parser\" --limit 5");

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

        var text = parseResult.GetRequiredValue(Opt<SearchTextArgument>.Instance);
        var limit = parseResult.GetValue(Opt<TaskLimitOption>.Instance);

        var filter = new TaskFilter
        {
            IncludeAll = true,
            ExcludeTombstones = true,
            Text = text,
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
