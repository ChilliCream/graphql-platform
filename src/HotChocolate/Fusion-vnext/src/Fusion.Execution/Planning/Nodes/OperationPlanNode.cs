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
}
