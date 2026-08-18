using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class AddTaskDependencyCommand : Command
{
    public AddTaskDependencyCommand() : base("add")
    {
        Description = "Add a dependency between two tasks.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Arguments.Add(Opt<DependsOnIdArgument>.Instance);

        Options.Add(Opt<TaskDependencyTypeOption>.Instance);
        Options.Add(Opt<TaskActorOption>.Instance);

        this.AddExamples(
            "task dep add \"acme-1a2\" \"acme-9z8\"",
            "task dep add \"acme-1a2\" \"acme-9z8\" --type waits-for");

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
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);
        var dependsOnId = parseResult.GetRequiredValue(Opt<DependsOnIdArgument>.Instance);
        var typeValue = parseResult.GetValue(Opt<TaskDependencyTypeOption>.Instance);
        var type = typeValue is null
            ? TaskDependencyTypes.Blocks
            : TaskDependencyTypes.Normalize(typeValue);
        var actor = TaskActor.Resolve(
            parseResult.GetValue(Opt<TaskActorOption>.Instance), environmentVariableProvider);
        var now = timeProvider.GetUtcNow();

        await using var connection = await store.ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await store.GetRequiredTaskAsync(connection, id, cancellationToken, transaction);
        await store.GetRequiredTaskAsync(connection, dependsOnId, cancellationToken, transaction);

        if (id == dependsOnId)
        {
            throw new ExitException("A task cannot depend on itself.");
        }

        var existingCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM dependencies WHERE task_id = @id AND depends_on_id = @dependsOnId",
            new { id, dependsOnId, cancellationToken },
            transaction);

        if (existingCount > 0)
        {
            throw new ExitException("Dependency already exists.");
        }

        await connection.ExecuteAsync(
            "INSERT INTO dependencies "
            + "(task_id, depends_on_id, dependency_type, created_at, created_by) "
            + "VALUES (@TaskId, @DependsOnId, @Type, @CreatedAt, @CreatedBy)",
            new TaskDependency
            {
                TaskId = id,
                DependsOnId = dependsOnId,
                Type = type,
                CreatedAt = now,
                CreatedBy = actor
            },
            transaction);

        await connection.ExecuteAsync(
            "UPDATE tasks SET updated_at = @updatedAt WHERE id = @id",
            new { updatedAt = now, id, cancellationToken },
            transaction);

        await store.RecordEventAsync(
            connection,
            new TaskEvent
            {
                TaskId = id,
                Type = TaskEventTypes.DependencyAdded,
                Actor = actor,
                OldValue = null,
                NewValue = $"{type}:{dependsOnId}",
                CreatedAt = now
            },
            cancellationToken,
            transaction);

        await transaction.CommitAsync(cancellationToken);

        console.OkLine(
            $"Added {type.EscapeMarkup()} dependency: "
            + $"'{id.EscapeMarkup()}' -> '{dependsOnId.EscapeMarkup()}'.");

        if (TaskDependencyTypes.IsBlocking(type))
        {
            var cycle = await FindBlockingCycleAsync(connection, id, dependsOnId);

            if (cycle is not null)
            {
                console.WriteLine($"Warning: dependency cycle: {FormatCycle(cycle)}");
            }
        }

        return ExitCodes.Success;
    }

    // Searches the existing blocking-dependency graph for a path from
    // dependsOnId back to id. Combined with the edge just inserted (id ->
    // dependsOnId), such a path closes a cycle.
    private static async Task<List<string>?> FindBlockingCycleAsync(
        SqliteConnection connection,
        string id,
        string dependsOnId)
    {
        var edges = (await connection.QueryAsync<EdgeRow>(
            "SELECT task_id AS TaskId, depends_on_id AS DependsOnId, dependency_type AS Type "
            + "FROM dependencies"))
            .Where(e => TaskDependencyTypes.IsBlocking(e.Type))
            .ToList();

        var adjacency = edges
            .GroupBy(e => e.TaskId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.DependsOnId).OrderBy(x => x, StringComparer.Ordinal).ToList());

        var predecessor = new Dictionary<string, string?> { [dependsOnId] = null };
        var queue = new Queue<string>();
        queue.Enqueue(dependsOnId);

        while (queue.TryDequeue(out var current))
        {
            if (current == id)
            {
                var path = new List<string>();
                string? node = current;

                while (node is not null)
                {
                    path.Add(node);
                    node = predecessor[node];
                }

                path.Reverse();

                var cycle = new List<string> { id };
                cycle.AddRange(path.Take(path.Count - 1));

                return cycle;
            }

            if (!adjacency.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var next in neighbors)
            {
                if (!predecessor.ContainsKey(next))
                {
                    predecessor[next] = current;
                    queue.Enqueue(next);
                }
            }
        }

        return null;
    }

    private static string FormatCycle(IReadOnlyList<string> cycle)
    {
        var minIndex = 0;

        for (var i = 1; i < cycle.Count; i++)
        {
            if (string.CompareOrdinal(cycle[i], cycle[minIndex]) < 0)
            {
                minIndex = i;
            }
        }

        var rotated = cycle.Skip(minIndex).Concat(cycle.Take(minIndex)).ToList();
        rotated.Add(rotated[0]);

        return string.Join(" -> ", rotated);
    }

    private sealed class EdgeRow
    {
        public required string TaskId { get; init; }
        public required string DependsOnId { get; init; }
        public required string Type { get; init; }
    }
}
