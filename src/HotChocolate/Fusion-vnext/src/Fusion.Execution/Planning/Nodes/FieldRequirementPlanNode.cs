using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed class FieldRequirementPlanNode : PlanNode
{
    public FieldRequirementPlanNode(
        string name,
        OperationPlanNode from,
        FieldPath selectionSet,
        FieldPath requiredField,
        ITypeNode type)
    {
        Name = name;
        From = from;
        SelectionSet = selectionSet;
        RequiredField = requiredField;
        Type = type;
    }

    public string Name { get; }

    public OperationPlanNode From { get; }

    public FieldPath SelectionSet { get; }

    public FieldPath RequiredField { get; }

    public ITypeNode Type { get; }
}
