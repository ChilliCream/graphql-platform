using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Represents an operation to resolve data from a specific source schema.
/// </summary>
public sealed class OperationPlanNode : SelectionPlanNode
{
    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType type,
        SelectionSetNode selectionSet,
        PlanNode? parent = null)
        : base(type, selectionSet.Selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public OperationPlanNode(
        string schemaName,
        ICompositeNamedType type,
        IReadOnlyList<ISelectionNode> selections,
        PlanNode? parent = null)
        : base(type, selections)
    {
        SchemaName = schemaName;
        Parent = parent;
    }

    public string SchemaName { get; }
}
