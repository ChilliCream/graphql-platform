using ChilliCream.Nitro.CommandLine.Commands.Tasks.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Tasks;

internal sealed class TreeTaskDependencyCommand : Command
{
    private const int MaxDepth = 10;

    public TreeTaskDependencyCommand() : base("tree")
    {
        Description = "Show a task's outgoing dependency tree.";

        Arguments.Add(Opt<TaskIdArgument>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent tasks dep tree \"acme-1a2\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<ITaskStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var id = parseResult.GetRequiredValue(Opt<TaskIdArgument>.Instance);

        var root = await store.GetRequiredTaskAsync(id, cancellationToken);

        var tasks = (await store.QueryTasksAsync(new TaskFilter { IncludeAll = true }, cancellationToken))
            .ToDictionary(t => t.Id);

        var edges = await store.GetDependencyEdgesAsync(cancellationToken);

        var childrenByParent = edges
            .GroupBy(e => e.TaskId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Type, StringComparer.Ordinal)
                    .ThenBy(e => e.DependsOnId, StringComparer.Ordinal)
                    .ToList());

        var printed = new HashSet<string> { root.Id };

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new TaskDependencyTreeNode
            {
                Id = root.Id,
                Type = null,
                Status = root.Status,
                Title = root.Title,
                Repeated = false,
                Children = BuildChildren(root.Id, tasks, childrenByParent, printed, depth: 1)
            }));

            return ExitCodes.Success;
        }

        console.WriteLine($"{root.Id} ({root.Status}) {root.Title}");

        WriteChildren(console, root.Id, tasks, childrenByParent, printed, depth: 1);

        return ExitCodes.Success;
    }

    private static IReadOnlyList<TaskDependencyTreeNode> BuildChildren(
        string parentId,
        IReadOnlyDictionary<string, TaskItem> tasks,
        IReadOnlyDictionary<string, List<TaskDependency>> childrenByParent,
        HashSet<string> printed,
        int depth)
    {
        if (depth > MaxDepth || !childrenByParent.TryGetValue(parentId, out var children))
        {
            return [];
        }

        var nodes = new List<TaskDependencyTreeNode>();

        foreach (var edge in children)
        {
            var alreadyPrinted = !printed.Add(edge.DependsOnId);
            var hasNode = tasks.TryGetValue(edge.DependsOnId, out var node);

            nodes.Add(new TaskDependencyTreeNode
            {
                Id = edge.DependsOnId,
                Type = edge.Type,
                Status = hasNode ? node!.Status : null,
                Title = hasNode ? node!.Title : null,
                Repeated = alreadyPrinted,
                Children = alreadyPrinted
                    ? []
                    : BuildChildren(edge.DependsOnId, tasks, childrenByParent, printed, depth + 1)
            });
        }

        return nodes;
    }

    private static void WriteChildren(
        INitroConsole console,
        string parentId,
        IReadOnlyDictionary<string, TaskItem> tasks,
        IReadOnlyDictionary<string, List<TaskDependency>> childrenByParent,
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
}
