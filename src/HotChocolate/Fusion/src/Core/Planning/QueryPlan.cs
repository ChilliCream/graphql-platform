using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly SelectionSetInfo _empty = new(0, Array.Empty<string>());
    private readonly Dictionary<ISelectionSet, SelectionSetInfo> _selectionSetInfos = new();
    private readonly HashSet<ISelectionSet> _hasNodes = new();
    private readonly QueryPlanNode[] _allNodes;

    public QueryPlan(
        IOperation operation,
        IEnumerable<QueryPlanNode> executionNodes,
        IEnumerable<ExportDefinition> exportDefinitions)
    {
        _allNodes = executionNodes.ToArray();

        foreach (var groupedKeys in exportDefinitions.GroupBy(t => t.SelectionSet, t => t.StateKey))
        {
            var nodesCount = 0;

            foreach (var node in _allNodes)
            {
                if (node.AppliesTo(groupedKeys.Key))
                {
                    nodesCount++;
                }
            }

            _selectionSetInfos.Add(groupedKeys.Key, new(nodesCount, groupedKeys.ToArray()));
        }

        foreach (var selectionSet in operation)
        {
            foreach (var node in _allNodes)
            {
                if (node.AppliesTo(selectionSet))
                {
                    _hasNodes.Add(selectionSet);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets all the nodes of this query plan.
    /// </summary>
    public IReadOnlyList<QueryPlanNode> AllNodes => _allNodes;

    /// <summary>
    /// Gets an info for the specified selection-set outlining the number of executable nodes
    /// and the state that these export.
    /// </summary>
    /// <param name="selectionSet">
    /// The selection-set for which the info shall be resolved.
    /// </param>
    /// <returns>
    /// Returns an info for the specified selection-set outlining the number of executable nodes
    /// and the state that these export.
    /// </returns>
    public SelectionSetInfo GetSelectionSetInfo(ISelectionSet selectionSet)
        => _selectionSetInfos.TryGetValue(selectionSet, out var value)
            ? value
            : _empty;

    public IEnumerable<QueryPlanNode> GetNodes(ISelectionSet selectionSet)
    {
        foreach (var node in _allNodes)
        {
            if (node.AppliesTo(selectionSet))
            {
                yield return node;
            }
        }
    }

    public bool HasNodes(ISelectionSet selectionSet)
        => _hasNodes.Contains(selectionSet);

    /// <summary>
    /// Gets the next executable nodes;
    /// </summary>
    /// <param name="completed">
    /// A set containing the already processed nodes.
    /// </param>
    /// <returns>
    /// Returns the nodes that need to be executed next.
    /// </returns>
    public IEnumerable<QueryPlanNode> GetNextNodes(ISet<QueryPlanNode> completed)
    {
        foreach (var node in _allNodes)
        {
            if (!completed.Contains(node) && completed.IsSupersetOf(node.DependsOn))
            {
                yield return node;
            }
        }
    }
}
