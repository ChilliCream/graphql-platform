using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

/// <summary>
/// Represents an operation to resolve data from a specific source schema.
/// </summary>
public sealed class OperationPlanNode : SelectionPlanNode, IOperationPlanNodeProvider
{
    private List<OperationPlanNode>? _operations;

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType declaringType,
        SelectionSetNode selectionSet,
        PlanNode? parent = null)
        : base(declaringType, selectionSet.Selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType declaringType,
        IReadOnlyList<ISelectionNode> selections,
        PlanNode? parent = null)
        : base(declaringType, selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public string SchemaName { get; }

    // todo: variable representations are missing.
    // todo: how to we represent state?

    public IReadOnlyList<OperationPlanNode> Operations
        => _operations ?? (IReadOnlyList<OperationPlanNode>)Array.Empty<OperationPlanNode>();

    public void AddOperation(OperationPlanNode operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        (_operations ??= []).Add(operation);
        operation.Parent = this;
    }

    public OperationDefinitionNode ToSyntaxNode()
    {
        return new OperationDefinitionNode(
            null,
            null,
            OperationType.Query,
            Array.Empty<VariableDefinitionNode>(),
            Directives.ToSyntaxNode(),
            Selections.ToSyntaxNode());
    }
}
