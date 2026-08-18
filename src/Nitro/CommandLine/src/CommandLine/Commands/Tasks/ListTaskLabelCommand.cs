using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskLabelCommand : Command
{
    public ListTaskLabelCommand() : base("list")
    {
        Description = "List a task's labels, or every label in use.";

        Arguments.Add(Opt<OptionalTaskIdArgument>.Instance);

        this.AddExamples("task label list", "task label list \"acme-1a2\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var id = parseResult.GetValue(Opt<OptionalTaskIdArgument>.Instance);

        await using var connection = await store.ConnectAsync(cancellationToken);

        if (id is not null)
        {
            var task = await store.GetRequiredTaskAsync(connection, id, cancellationToken);

            var labels = (await connection.QueryAsync<string>(
                "SELECT label FROM labels WHERE task_id = @id ORDER BY label",
                new { id = task.Id, cancellationToken })).ToList();

            if (labels.Count == 0)
            {
                console.WriteLine("No labels.");
                return ExitCodes.Success;
            }

            foreach (var label in labels)
            {
                console.WriteLine(label);
            }

            return ExitCodes.Success;
        }

        var rows = (await connection.QueryAsync<LabelCountRow>(
            """
            SELECT l.label AS Label, COUNT(*) AS Count
            FROM labels l
            JOIN tasks t ON t.id = l.task_id
            WHERE t.status != @tombstoneStatus
            GROUP BY l.label
            ORDER BY l.label
            """,
            new { tombstoneStatus = TaskStates.Tombstone })).ToList();

        if (rows.Count == 0)
        {
            console.WriteLine("No labels.");
            return ExitCodes.Success;
        }

        foreach (var row in rows)
        {
            console.WriteLine($"{row.Label}  {row.Count}");
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// A label's name and how many non-tombstone tasks carry it.
    /// </summary>
    private sealed class LabelCountRow
    {
        public required string Label { get; init; }
        public required int Count { get; init; }
    }
}
