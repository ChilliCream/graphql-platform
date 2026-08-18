using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ListTaskCommand : Command
{
    public ListTaskCommand() : base("list")
    {
        Description = "List tasks.";

        Options.Add(Opt<TaskStatusFilterOption>.Instance);
        Options.Add(Opt<TaskTypeOption>.Instance);
        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskAssigneeOption>.Instance);
        Options.Add(Opt<TaskLabelOption>.Instance);
        Options.Add(Opt<TaskLimitOption>.Instance);
        Options.Add(Opt<TaskAllOption>.Instance);

        this.AddExamples(
            "task list",
            "task list --status open --status in_progress",
            "task list --assignee alice --priority p1");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var statuses = parseResult.GetValue(Opt<TaskStatusFilterOption>.Instance);
        var type = parseResult.GetValue(Opt<TaskTypeOption>.Instance);
        var priority = parseResult.GetValue(Opt<TaskPriorityOption>.Instance);
        var assignee = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance);
        var labels = parseResult.GetValue(Opt<TaskLabelOption>.Instance);
        var limit = parseResult.GetValue(Opt<TaskLimitOption>.Instance);
        var all = parseResult.GetValue(Opt<TaskAllOption>.Instance);

        var (whereClause, parameters) = BuildWhereClause(statuses, type, priority, assignee, labels, all);

        var sql = "SELECT id AS Id, priority AS Priority, task_type AS Type, "
            + $"status AS Status, title AS Title FROM tasks{whereClause}"
            + " ORDER BY priority ASC, created_at ASC, id ASC";

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

    private static (string WhereClause, DynamicParameters Parameters) BuildWhereClause(
        string[]? statuses,
        string? type,
        string? priority,
        string? assignee,
        string[]? labels,
        bool all)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (statuses is { Length: > 0 })
        {
            parameters.Add("statuses", statuses.Select(TaskStates.Normalize).ToArray());
            conditions.Add("status IN @statuses");
        }
        else if (!all)
        {
            parameters.Add("closedStatus", TaskStates.Closed);
            parameters.Add("tombstoneStatus", TaskStates.Tombstone);
            conditions.Add("status NOT IN (@closedStatus, @tombstoneStatus)");
        }

        if (!string.IsNullOrEmpty(type))
        {
            parameters.Add("type", TaskTypes.Normalize(type));
            conditions.Add("task_type = @type");
        }

        if (!string.IsNullOrEmpty(assignee))
        {
            parameters.Add("assignee", assignee);
            conditions.Add("assignee = @assignee");
        }

        if (!string.IsNullOrEmpty(priority))
        {
            parameters.Add("priority", TaskPriorities.Parse(priority));
            conditions.Add("priority = @priority");
        }

        if (labels is { Length: > 0 })
        {
            for (var i = 0; i < labels.Length; i++)
            {
                var parameterName = $"label{i}";
                parameters.Add(parameterName, labels[i].Trim().ToLowerInvariant());
                conditions.Add(
                    "EXISTS (SELECT 1 FROM labels WHERE task_id = tasks.id "
                    + $"AND label = @{parameterName})");
            }
        }

        var whereClause = conditions.Count > 0 ? $" WHERE {string.Join(" AND ", conditions)}" : "";

        return (whereClause, parameters);
    }
}
