using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class StatsTaskCommand : Command
{
    private static readonly string[] StatusOrder =
    [
        TaskStates.Open,
        TaskStates.InProgress,
        TaskStates.Blocked,
        TaskStates.Deferred,
        TaskStates.Closed,
        TaskStates.Tombstone
    ];

    public StatsTaskCommand() : base("stats")
    {
        Description = "Show summary statistics for the task workspace.";

        this.AddExamples("task stats");

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

        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);

        var statusCounts = (await connection.QueryAsync<StatusCountRow>(
            "SELECT status AS Status, COUNT(*) AS Count FROM tasks "
            + "WHERE status != @tombstone GROUP BY status",
            new { tombstone = TaskStates.Tombstone })).ToList();

        var totalTasks = statusCounts.Sum(row => row.Count);

        var blocked = await store.ComputeBlockedAsync(connection, cancellationToken);

        var readyIds = await connection.QueryAsync<string>(
            "SELECT id FROM tasks WHERE status = @status "
            + "AND (defer_until IS NULL OR defer_until <= @now)",
            new { status = TaskStates.Open, now, cancellationToken });

        var readyCount = readyIds.Count(id => !blocked.ContainsKey(id));

        var blockedCount = 0;

        if (blocked.Count > 0)
        {
            // The IN clause needs the array-expansion that only the
            // reflection (CommandDefinition) path performs; the intercepted
            // classic shape sends "@ids" verbatim and SQLite rejects it.
            var blockedStatuses = await connection.QueryAsync<string>(
                new CommandDefinition(
                    "SELECT status FROM tasks WHERE id IN @ids",
                    new { ids = blocked.Keys.ToArray() },
                    cancellationToken: cancellationToken));

            blockedCount = blockedStatuses.Count(status => !TaskStates.IsTerminal(status));
        }

        var labelCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT l.label) FROM labels l "
            + "INNER JOIN tasks t ON t.id = l.task_id WHERE t.status != @tombstone",
            new { tombstone = TaskStates.Tombstone, cancellationToken });

        var commentCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM comments");

        var eventCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM events");

        console.WriteLine($"Tasks: {totalTasks}");

        var orderedStatusCounts = statusCounts
            .OrderBy(StatusOrderIndex)
            .ThenBy(statusRow => statusRow.Status, StringComparer.Ordinal);

        foreach (var row in orderedStatusCounts)
        {
            console.WriteLine($"  {row.Status}: {row.Count}");
        }

        console.WriteLine($"Ready: {readyCount}");
        console.WriteLine($"Blocked: {blockedCount}");
        console.WriteLine($"Labels: {labelCount}");
        console.WriteLine($"Comments: {commentCount}");
        console.WriteLine($"Events: {eventCount}");

        return ExitCodes.Success;
    }

    private static int StatusOrderIndex(StatusCountRow row)
    {
        var index = Array.IndexOf(StatusOrder, row.Status);

        return index < 0 ? StatusOrder.Length : index;
    }

    /// <summary>
    /// One status group and how many non-tombstone tasks currently have it.
    /// </summary>
    private sealed class StatusCountRow
    {
        public required string Status { get; init; }
        public required int Count { get; init; }
    }
}
