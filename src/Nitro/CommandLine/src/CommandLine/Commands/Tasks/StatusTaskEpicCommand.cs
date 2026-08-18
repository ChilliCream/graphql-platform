using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class StatusTaskEpicCommand : Command
{
    public StatusTaskEpicCommand() : base("status")
    {
        Description = "Show epics with their child completion counts.";

        this.AddExamples("task epic status");

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

        var epics = await GetEpicsAsync(connection, cancellationToken);

        if (epics.Count == 0)
        {
            console.WriteLine("No epics found.");
            return ExitCodes.Success;
        }

        foreach (var epic in epics)
        {
            var line = $"{epic.Id}  {epic.Closed}/{epic.Total}  {epic.Title}";

            if (epic.IsEligibleForClose)
            {
                line += "  (eligible for close)";
            }

            console.WriteLine(line);
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Loads every non-tombstone epic with its direct, non-tombstone child
    /// count and how many of those children are closed.
    /// </summary>
    internal static async Task<List<EpicRow>> GetEpicsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
        => (await connection.QueryAsync<EpicRow>(
            new CommandDefinition(
                """
                SELECT e.id AS Id, e.title AS Title, e.status AS Status,
                       COUNT(c.id) AS Total,
                       SUM(CASE WHEN c.status = @closed THEN 1 ELSE 0 END) AS Closed
                FROM tasks e
                LEFT JOIN dependencies d
                    ON d.depends_on_id = e.id AND d.dependency_type = @parentChild
                LEFT JOIN tasks c
                    ON c.id = d.task_id AND c.status != @tombstone
                WHERE e.task_type = @epic AND e.status != @tombstone
                GROUP BY e.id, e.title, e.status
                ORDER BY e.id
                """,
                new
                {
                    closed = TaskStates.Closed,
                    parentChild = TaskDependencyTypes.ParentChild,
                    tombstone = TaskStates.Tombstone,
                    epic = TaskTypes.Epic
                },
                cancellationToken: cancellationToken))).ToList();

    /// <summary>
    /// An epic's direct child completion counts. <see cref="Total"/> and
    /// <see cref="Closed"/> count only non-tombstone children.
    /// </summary>
    internal sealed class EpicRow
    {
        public required string Id { get; init; }
        public required string Title { get; init; }
        public required string Status { get; init; }
        public required int Total { get; init; }
        public required int Closed { get; init; }

        /// <summary>
        /// An epic is eligible for close when it has at least one child, all
        /// of its children are closed, and the epic itself is not already
        /// closed.
        /// </summary>
        public bool IsEligibleForClose
            => Total > 0 && Closed == Total && Status != TaskStates.Closed;
    }
}
