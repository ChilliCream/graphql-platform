namespace HotChocolate.Fusion.Planning.Nodes;

public interface IOperationPlanNodeProvider
{
    public IReadOnlyList<OperationPlanNode> Operations { get; }
}
