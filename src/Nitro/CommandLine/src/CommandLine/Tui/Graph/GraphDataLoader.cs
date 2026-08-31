using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Loads the complete current task graph from the task store. Loading is
/// read-only and returns a model without applying presentation reductions.
/// </summary>
internal sealed class GraphDataLoader(ITaskStore store)
{
    public async Task<GraphModel> LoadAsync(CancellationToken cancellationToken)
    {
        var tasks = await store.QueryTasksAsync(
            new TaskFilter
            {
                IncludeAll = true,
                IncludeArchived = true,
                ExcludeTombstones = true
            },
            cancellationToken);
        var dependencies = await store.GetDependencyEdgesAsync(cancellationToken);
        var nodes = new List<GraphNode>(tasks.Count);

        foreach (var task in tasks)
        {
            var labels = await store.GetLabelsAsync(task.Id, cancellationToken);
            nodes.Add(GraphNode.FromTask(task, labels));
        }

        var nodeIds = nodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var edges = new List<GraphEdge>();

        foreach (var dependency in dependencies)
        {
            if (!nodeIds.Contains(dependency.TaskId)
                || !nodeIds.Contains(dependency.DependsOnId)
                || dependency.TaskId == dependency.DependsOnId)
            {
                continue;
            }

            if (dependency.Type == TaskDependencyTypes.ParentChild)
            {
                edges.Add(new GraphEdge(
                    dependency.DependsOnId,
                    dependency.TaskId,
                    GraphEdgeKind.ParentChild));
            }
            else if (TaskDependencyTypes.IsBlocking(dependency.Type))
            {
                edges.Add(new GraphEdge(
                    dependency.DependsOnId,
                    dependency.TaskId,
                    GraphEdgeKind.Blocks));
            }
        }

        return GraphReducer.Order(new GraphModel(nodes, edges));
    }
}
