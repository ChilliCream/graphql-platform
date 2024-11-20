namespace HotChocolate.Fusion.Planning;

public interface IOperationPlanNodeProvider
{
    public IReadOnlyList<OperationPlanNode> Operations { get; }
}
