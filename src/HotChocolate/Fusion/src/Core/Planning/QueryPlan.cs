using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly ILookup<ISelectionSet, RequestNode> _lookup;
    private readonly Dictionary<ISelectionSet, string[]> _exports;

    public QueryPlan(
        IEnumerable<ExecutionNode> executionNodes,
        IEnumerable<ExportDefinition> exportDefinitions)
    {
        ExecutionNodes = executionNodes.ToArray();
        RootExecutionNodes = ExecutionNodes.Where(t => t.DependsOn.Count == 0).ToArray();
        _lookup = ExecutionNodes.OfType<RequestNode>().ToLookup(t => t.Handler.SelectionSet);

        _exports = exportDefinitions
            .GroupBy(t => t.SelectionSet, t => t.StateKey)
            .ToDictionary(t => t.Key, t => t.ToArray());
    }

    public IReadOnlyList<ExecutionNode> RootExecutionNodes { get; }

    public IReadOnlyList<ExecutionNode> ExecutionNodes { get; }

    public IEnumerable<RequestNode> GetRequestNodes(ISelectionSet selectionSet)
        => _lookup[selectionSet];

    public IReadOnlyList<string> GetExports(ISelectionSet selectionSet)
    {
        if (_exports.TryGetValue(selectionSet, out var exportKeys))
        {
            return exportKeys;
        }

        return Array.Empty<string>();
    }
}
