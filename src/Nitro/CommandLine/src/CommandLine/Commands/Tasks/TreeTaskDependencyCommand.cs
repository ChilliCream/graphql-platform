using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using Dapper;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class TreeTaskDependencyCommand : Command
{
    private const int MaxDepth = 10;

    public TreeTaskDependencyCommand() : base("tree")
    {
        Description = "Show a task's outgoing dependency tree.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);

        this.AddExamples("task dep tree \"acme-1a2\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);

        await using var connection = await store.ConnectAsync(cancellationToken);
        var root = await store.GetRequiredTaskAsync(connection, id, cancellationToken);

        var tasks = (await connection.QueryAsync<TaskNodeRow>(
            "SELECT id AS Id, status AS Status, title AS Title FROM tasks"))
            .ToDictionary(t => t.Id);

        var edges = (await connection.QueryAsync<DependencyEdgeRow>(
            "SELECT task_id AS TaskId, depends_on_id AS DependsOnId, dependency_type AS Type "
            + "FROM dependencies")).ToList();

        var childrenByParent = edges
            .GroupBy(e => e.TaskId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Type, StringComparer.Ordinal)
                    .ThenBy(e => e.DependsOnId, StringComparer.Ordinal)
                    .ToList());

        var printed = new HashSet<string> { root.Id };

        console.WriteLine($"{root.Id} ({root.Status}) {root.Title}");

        WriteChildren(console, root.Id, tasks, childrenByParent, printed, depth: 1);

        return ExitCodes.Success;
    }

    private static void WriteChildren(
        INitroConsole console,
        string parentId,
        IReadOnlyDictionary<string, TaskNodeRow> tasks,
        IReadOnlyDictionary<string, List<DependencyEdgeRow>> childrenByParent,
        HashSet<string> printed,
        int depth)
    {
        if (depth > MaxDepth || !childrenByParent.TryGetValue(parentId, out var children))
        {
            return;
        }

        var indent = new string(' ', depth * 2);

        foreach (var edge in children)
        {
            var alreadyPrinted = !printed.Add(edge.DependsOnId);
            var hasNode = tasks.TryGetValue(edge.DependsOnId, out var node);
            var status = hasNode ? node!.Status : "unknown";
            var title = hasNode ? node!.Title : "";
            var line = $"{indent}{edge.DependsOnId} ({edge.Type}, {status}) {title}";

            if (alreadyPrinted)
            {
                line += " *";
            }

            console.WriteLine(line);

            if (!alreadyPrinted)
            {
                WriteChildren(console, edge.DependsOnId, tasks, childrenByParent, printed, depth + 1);
            }
        }
    }

    private sealed class TaskNodeRow
    {
        public required string Id { get; init; }
        public required string Status { get; init; }
        public required string Title { get; init; }
    }

    private sealed class DependencyEdgeRow
    {
        public required string TaskId { get; init; }
        public required string DependsOnId { get; init; }
        public required string Type { get; init; }
    }
}
