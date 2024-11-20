namespace HotChocolate.Fusion.Planning;

public sealed class RootPlanNode : PlanNode, IOperationPlanNodeProvider
{
    private readonly List<OperationPlanNode> _operations = new();

    public IReadOnlyList<OperationPlanNode> Operations
        => _operations;

    public void AddOperation(OperationPlanNode operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _operations.Add(operation);
        operation.Parent = this;
    }
}
