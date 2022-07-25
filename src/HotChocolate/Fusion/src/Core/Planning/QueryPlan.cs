using HotChocolate.Execution.Processing;

namespace HotChocolate.Fusion.Planning;

internal class QueryPlan
{
    public QueryPlan(IReadOnlyList<ExecutionNode> executionNodes)
    {
        ExecutionNodes = executionNodes;
        RootExecutionNodes = executionNodes.Where(t => t.DependsOn.Count == 0).ToArray();
    }

    public IReadOnlyList<ExecutionNode> RootExecutionNodes { get; }

    public IReadOnlyList<ExecutionNode> ExecutionNodes { get; }

    public IEnumerable<RequestNode> GetRequestNodes(ISelectionSet selectionSet);
}
