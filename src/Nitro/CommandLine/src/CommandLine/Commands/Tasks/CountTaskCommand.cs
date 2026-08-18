using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class CountTaskCommand : Command
{
    public CountTaskCommand() : base("count")
    {
        Description = "Count tasks.";

        Options.Add(Opt<TaskByOption>.Instance);

        this.AddExamples("task count", "task count --by status");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var by = parseResult.GetValue(Opt<TaskByOption>.Instance);

        await using var connection = await store.ConnectAsync(cancellationToken);

        if (string.IsNullOrEmpty(by))
        {
            var total = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM tasks WHERE status != @tombstone",
                new { tombstone = TaskStates.Tombstone, cancellationToken });

            console.WriteLine(total.ToString());

            return ExitCodes.Success;
        }

        var rows = await GetCountsAsync(connection, by);

        foreach (var (value, count) in rows)
        {
            console.WriteLine($"{value}  {count}");
        }

        return ExitCodes.Success;
    }

    private static async Task<IReadOnlyList<(string Value, int Count)>> GetCountsAsync(
        SqliteConnection connection,
        string by)
    {
        switch (by.Trim().ToLowerInvariant())
        {
            case "status":
                return (await connection.QueryAsync<CountRow>(
                        "SELECT status AS Value, COUNT(*) AS Count FROM tasks "
                        + "WHERE status != @tombstone GROUP BY status ORDER BY status ASC",
                        new { tombstone = TaskStates.Tombstone }))
                    .Select(r => (r.Value, r.Count))
                    .ToList();

            case "type":
                return (await connection.QueryAsync<CountRow>(
                        "SELECT task_type AS Value, COUNT(*) AS Count FROM tasks "
                        + "WHERE status != @tombstone GROUP BY task_type ORDER BY task_type ASC",
                        new { tombstone = TaskStates.Tombstone }))
                    .Select(r => (r.Value, r.Count))
                    .ToList();

            case "priority":
                return (await connection.QueryAsync<PriorityCountRow>(
                        "SELECT priority AS Priority, COUNT(*) AS Count FROM tasks "
                        + "WHERE status != @tombstone GROUP BY priority ORDER BY priority ASC",
                        new { tombstone = TaskStates.Tombstone }))
                    .Select(r => (TaskPriorities.Format(r.Priority), r.Count))
                    .ToList();

            case "assignee":
                return (await connection.QueryAsync<CountRow>(
                        "SELECT COALESCE(NULLIF(assignee, ''), 'unassigned') AS Value, "
                        + "COUNT(*) AS Count FROM tasks WHERE status != @tombstone "
                        + "GROUP BY COALESCE(NULLIF(assignee, ''), 'unassigned') "
                        + "ORDER BY COALESCE(NULLIF(assignee, ''), 'unassigned') ASC",
                        new { tombstone = TaskStates.Tombstone }))
                    .Select(r => (r.Value, r.Count))
                    .ToList();

            case "label":
                return (await connection.QueryAsync<CountRow>(
                        "SELECT label AS Value, COUNT(*) AS Count FROM labels "
                        + "INNER JOIN tasks ON tasks.id = labels.task_id "
                        + "WHERE tasks.status != @tombstone GROUP BY label ORDER BY label ASC",
                        new { tombstone = TaskStates.Tombstone }))
                    .Select(r => (r.Value, r.Count))
                    .ToList();

            default:
                throw new ExitException(
                    $"Invalid --by value '{by}'. Use status, type, priority, assignee, or label.");
        }
    }

    /// <summary>
    /// One group-by-value row: the grouped value and how many tasks fall into it.
    /// </summary>
    private sealed class CountRow
    {
        public required string Value { get; init; }
        public required int Count { get; init; }
    }

    /// <summary>
    /// A group-by-priority row, kept numeric so it can be formatted as P0..P4.
    /// </summary>
    private sealed class PriorityCountRow
    {
        public required int Priority { get; init; }
        public required int Count { get; init; }
    }
}
