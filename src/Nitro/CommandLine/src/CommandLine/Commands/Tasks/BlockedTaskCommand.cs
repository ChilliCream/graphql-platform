using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class BlockedTaskCommand : Command
{
    public BlockedTaskCommand() : base("blocked")
    {
        Description = "List tasks that are blocked by unfinished dependencies.";

        this.AddExamples("task blocked");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var blocked = await store.ComputeBlockedAsync(cancellationToken);

        if (blocked.Count == 0)
        {
            console.WriteLine("No blocked tasks.");
            return ExitCodes.Success;
        }

        var tasks = new List<TaskItem>();

        foreach (var id in blocked.Keys)
        {
            var task = await store.GetTaskAsync(id, cancellationToken);

            if (task is not null && !TaskStates.IsTerminal(task.Status))
            {
                tasks.Add(task);
            }
        }

        tasks = tasks.OrderBy(t => t.Id, StringComparer.Ordinal).ToList();

        if (tasks.Count == 0)
        {
            console.WriteLine("No blocked tasks.");
            return ExitCodes.Success;
        }

        foreach (var task in tasks)
        {
            console.WriteLine(FormatRow(task, blocked[task.Id]));
        }

        console.WriteLine();
        console.WriteLine($"{tasks.Count} task(s)");

        return ExitCodes.Success;
    }

    private static string FormatRow(TaskItem task, IReadOnlyList<string> blockers)
        => $"{task.Id}  {TaskPriorities.Format(task.Priority)}  {task.Type}  "
            + $"{task.Status}  {task.Title}  "
            + $"(blocked by: {string.Join(", ", blockers)})";
}
