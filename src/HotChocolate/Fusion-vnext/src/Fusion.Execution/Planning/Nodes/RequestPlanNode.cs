using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class RequestPlanNode : PlanNode
{
    private readonly List<OperationPlanNode> _operations = [];

    public RequestPlanNode(DocumentNode document, string? operationName = null)
    {
        Document = document;
        OperationName = operationName;
    }

    public DocumentNode Document { get; }

    public string? OperationName { get; }

    public IReadOnlyList<OperationPlanNode> Operations => _operations;

    public void AddOperation(OperationPlanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _operations.Add(node);
        node.Parent = this;
    }
}
