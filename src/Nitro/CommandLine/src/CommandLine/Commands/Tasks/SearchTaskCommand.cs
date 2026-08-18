using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class SearchTaskCommand : Command
{
    public SearchTaskCommand() : base("search")
    {
        Description = "Search tasks by text.";

        Arguments.Add(Opt<SearchTextArgument>.Instance);
        Options.Add(Opt<TaskLimitOption>.Instance);

        this.AddExamples("task search \"parser\"", "task search \"parser\" --limit 5");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var text = parseResult.GetRequiredValue(Opt<SearchTextArgument>.Instance);
        var limit = parseResult.GetValue(Opt<TaskLimitOption>.Instance);

        var parameters = new DynamicParameters();
        parameters.Add("tombstone", TaskStates.Tombstone);
        parameters.Add("text", EscapeLikeText(text));

        var sql = "SELECT id AS Id, priority AS Priority, task_type AS Type, "
            + "status AS Status, title AS Title FROM tasks WHERE status != @tombstone AND ("
            + "LOWER(title) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
            + "LOWER(description) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
            + "LOWER(design) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
            + "LOWER(acceptance_criteria) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\' OR "
            + "LOWER(notes) LIKE '%' || LOWER(@text) || '%' ESCAPE '\\') "
            + "ORDER BY priority ASC, created_at ASC, id ASC";

        if (limit is { } limitValue)
        {
            parameters.Add("limit", limitValue);
            sql += " LIMIT @limit";
        }

        await using var connection = await store.ConnectAsync(cancellationToken);

        var tasks = (await connection.QueryAsync<TaskListRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        if (tasks.Count == 0)
        {
            console.WriteLine("No tasks found.");
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

    /// <summary>
    /// Escapes the LIKE wildcard characters '%' and '_' (and the escape
    /// character itself) so the search text is matched literally, other than
    /// the wildcards this command wraps around it.
    /// </summary>
    private static string EscapeLikeText(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
