namespace HotChocolate.Fusion;

internal class QueryPlan
{
    public QueryPlan(IReadOnlyList<ExecutionNode> executionNodes)
    {
        ExecutionNodes = executionNodes;
    }

    public IReadOnlyList<ExecutionNode> ExecutionNodes { get; }
}
