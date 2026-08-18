using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

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

        await using var connection = await store.ConnectAsync(cancellationToken);

        var blocked = await store.ComputeBlockedAsync(connection, cancellationToken);

        if (blocked.Count == 0)
        {
            console.WriteLine("No blocked tasks.");
            return ExitCodes.Success;
        }

        // The IN clause needs the array-expansion that only the reflection
        // (CommandDefinition) path performs; the intercepted classic shape
        // sends "@ids" verbatim and SQLite rejects it.
        var tasks = (await connection.QueryAsync<TaskRow>(
                new CommandDefinition(
                    "SELECT id AS Id, priority AS Priority, task_type AS Type, "
                    + "status AS Status, title AS Title FROM tasks WHERE id IN @ids ORDER BY id",
                    new { ids = blocked.Keys.ToArray() },
                    cancellationToken: cancellationToken)))
            .Where(t => !TaskStates.IsTerminal(t.Status))
            .ToList();

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

    private static string FormatRow(TaskRow task, IReadOnlyList<string> blockers)
        => $"{task.Id}  {TaskPriorities.Format(task.Priority)}  {task.Type}  "
            + $"{task.Status}  {task.Title}  "
            + $"(blocked by: {string.Join(", ", blockers)})";

    /// <summary>
    /// The subset of a task's columns needed to print one blocked-task row.
    /// </summary>
    private sealed class TaskRow
    {
        public required string Id { get; init; }
        public required int Priority { get; init; }
        public required string Type { get; init; }
        public required string Status { get; init; }
        public required string Title { get; init; }
    }
}
