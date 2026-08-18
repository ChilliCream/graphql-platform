using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

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

        await using var connection = await store.ConnectAsync(cancellationToken);

        var tasks = (await connection.QueryAsync<TaskListRow>(
            "SELECT id AS Id, priority AS Priority, task_type AS Type, "
            + "status AS Status, title AS Title FROM tasks "
            + "WHERE status IN (@open, @inProgress) AND updated_at <= @threshold "
            + "ORDER BY updated_at ASC, id ASC",
            new
            {
                open = TaskStates.Open,
                inProgress = TaskStates.InProgress,
                threshold
            })).ToList();

        if (tasks.Count == 0)
        {
            console.WriteLine("No stale tasks.");
            return ExitCodes.Success;
        }

        foreach (var task in tasks)
        {
            console.WriteLine(task.Format());
        }

        console.WriteLine();
        console.WriteLine($"{tasks.Count} task(s)");

        return ExitCodes.Success;
    }
}
