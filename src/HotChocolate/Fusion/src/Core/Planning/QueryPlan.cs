using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly ILookup<ISelectionSet, RequestNode> _lookup;
    private readonly Dictionary<ISelectionSet, string[]> _exports;

    public QueryPlan(
        IEnumerable<ExecutionNode> executionNodes,
        IEnumerable<ExportDefinition> exportDefinitions,
        bool hasIntrospectionSelections)
    {
        ExecutionNodes = executionNodes.ToArray();
        RootExecutionNodes = ExecutionNodes.Where(t => t.DependsOn.Count == 0).ToArray();
        RequiresFetch = new HashSet<ISelectionSet>(ExecutionNodes.OfType<RequestNode>().Select(t => t.Handler.SelectionSet));
        HasIntrospectionSelections = hasIntrospectionSelections;

        _lookup = ExecutionNodes.OfType<RequestNode>().ToLookup(t => t.Handler.SelectionSet);
        _exports = exportDefinitions.GroupBy(t => t.SelectionSet, t => t.StateKey).ToDictionary(t => t.Key, t => t.ToArray());
    }

    public bool HasIntrospectionSelections { get; set; }

    public IReadOnlyList<ExecutionNode> RootExecutionNodes { get; }

    public IReadOnlyList<ExecutionNode> ExecutionNodes { get; }

    // name is not really good... the selection sets that require execution of request nodes.
    public IReadOnlySet<ISelectionSet> RequiresFetch { get; }

    // should we return a tree instead so that dependencies are correctly modeled?
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
