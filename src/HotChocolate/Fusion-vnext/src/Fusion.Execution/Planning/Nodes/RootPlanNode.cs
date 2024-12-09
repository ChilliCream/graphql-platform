namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class RootPlanNode : PlanNode
{
    private readonly List<OperationPlanNode> _operations = [];

    public IReadOnlyList<OperationPlanNode> Operations => _operations;

    public void AddOperation(OperationPlanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _operations.Add(node);
        node.Parent = this;
    }
}
