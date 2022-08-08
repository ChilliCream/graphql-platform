using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal sealed class QueryPlan
{
    private readonly ILookup<ISelectionSet, RequestNode> _lookup;

    public QueryPlan(IReadOnlyList<ExecutionNode> executionNodes)
    {
        ExecutionNodes = executionNodes;
        RootExecutionNodes = executionNodes.Where(t => t.DependsOn.Count == 0).ToArray();
        _lookup = executionNodes.OfType<RequestNode>().ToLookup(t => t.Handler.SelectionSet);
    }

    public IReadOnlyList<ExecutionNode> RootExecutionNodes { get; }

    public IReadOnlyList<ExecutionNode> ExecutionNodes { get; }

    public IEnumerable<RequestNode> GetRequestNodes(ISelectionSet selectionSet)
        => _lookup[selectionSet];
}
