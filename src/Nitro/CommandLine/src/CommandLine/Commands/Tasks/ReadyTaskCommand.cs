using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class ReadyTaskCommand : Command
{
    public ReadyTaskCommand() : base("ready")
    {
        Description = "List tasks that are ready to work on.";

        Options.Add(Opt<TaskPriorityOption>.Instance);
        Options.Add(Opt<TaskAssigneeOption>.Instance);
        Options.Add(Opt<TaskLabelOption>.Instance);
        Options.Add(Opt<TaskLimitOption>.Instance);
        Options.Add(Opt<TaskIncludeDeferredOption>.Instance);

        this.AddExamples("task ready", "task ready --assignee alice --limit 5");

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

        var priorityValue = parseResult.GetValue(Opt<TaskPriorityOption>.Instance);
        var assignee = parseResult.GetValue(Opt<TaskAssigneeOption>.Instance);
        var labels = parseResult.GetValue(Opt<TaskLabelOption>.Instance) ?? [];
        var limit = parseResult.GetValue(Opt<TaskLimitOption>.Instance);
        var includeDeferred = parseResult.GetValue(Opt<TaskIncludeDeferredOption>.Instance);

        int? priority = priorityValue is null ? null : TaskPriorities.Parse(priorityValue);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);

        var blocked = await store.ComputeBlockedAsync(connection, cancellationToken);

        var sql = $"SELECT {TaskItem.Columns} FROM tasks WHERE status = @status";
        var parameters = new DynamicParameters();
        parameters.Add("status", TaskStates.Open);

        if (priority is { } priorityFilter)
        {
            sql += " AND priority = @priority";
            parameters.Add("priority", priorityFilter);
        }

        if (!string.IsNullOrEmpty(assignee))
        {
            if (string.Equals(assignee, "unassigned", StringComparison.OrdinalIgnoreCase))
            {
                sql += " AND (assignee IS NULL OR assignee = '')";
            }
            else
            {
                sql += " AND assignee = @assignee";
                parameters.Add("assignee", assignee);
            }
        }

        for (var i = 0; i < labels.Length; i++)
        {
            var parameterName = $"label{i}";
            sql +=
                $" AND EXISTS (SELECT 1 FROM labels WHERE labels.task_id = tasks.id AND labels.label = @{parameterName})";
            parameters.Add(parameterName, labels[i].Trim().ToLowerInvariant());
        }

        if (!includeDeferred)
        {
            sql += " AND (defer_until IS NULL OR defer_until <= @now)";
            parameters.Add("now", now);
        }

        sql += " ORDER BY CASE WHEN priority <= 1 THEN 0 ELSE 1 END, created_at ASC, id ASC";

        var tasks = (await connection.QueryAsync<TaskItem>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)))
            .Where(t => !blocked.ContainsKey(t.Id));

        if (limit is { } limitValue)
        {
            tasks = tasks.Take(limitValue);
        }

        var readyTasks = tasks.ToList();

        if (readyTasks.Count == 0)
        {
            console.WriteLine("No ready tasks.");
            return ExitCodes.Success;
        }

        foreach (var task in readyTasks)
        {
            console.WriteLine(FormatRow(task));
        }

        console.WriteLine();
        console.WriteLine($"{readyTasks.Count} task(s)");

        return ExitCodes.Success;
    }

    private static string FormatRow(TaskItem task)
        => $"{task.Id}  {TaskPriorities.Format(task.Priority)}  {task.Type}  "
            + $"{task.Status}  {task.Title}";
}
