using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

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

    public DocumentNode ToSyntaxNode()
    {
        var backlog = new Queue<OperationPlanNode>();
        var definitions = ImmutableArray.CreateBuilder<IDefinitionNode>();

        foreach(var operation in _operations)
        {
            backlog.Enqueue(operation);
        }

        while(backlog.TryDequeue(out var operation))
        {
            definitions.Add(operation.ToSyntaxNode());

            foreach(var child in operation.Operations)
            {
                backlog.Enqueue(child);
            }
        }

        return new DocumentNode(definitions.ToImmutable());
    }
}
