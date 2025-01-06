using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes;

public sealed record DataRequirementPlanNode : IParentPlanNodeProvider
{
    public DataRequirementPlanNode(
        string name,
        SelectionPath requiredField,
        ITypeNode type)
    {
        Name = name;
        RequiredField = requiredField;
        Type = type;
    }

    public DataRequirementPlanNode(
        string name,
        OperationPlanNode dependsOn,
        SelectionPath selectionSet,
        SelectionPath requiredField,
        ITypeNode type)
    {
        Name = name;
        DependsOn = ImmutableArray<OperationPlanNode>.Empty.Add(dependsOn);
        SelectionSet = selectionSet;
        RequiredField = requiredField;
        Type = type;
    }

    public DataRequirementPlanNode(
        string name,
        ImmutableArray<OperationPlanNode> dependsOn,
        SelectionPath selectionSet,
        SelectionPath requiredField,
        ITypeNode type)
    {
        Name = name;
        DependsOn = dependsOn;
        SelectionSet = selectionSet;
        RequiredField = requiredField;
        Type = type;
    }

    public PlanNode? Parent { get; init; }
    public string Name { get; init; }
    public ImmutableArray<OperationPlanNode>? DependsOn { get; init; }
    public SelectionPath? SelectionSet { get; init; }
    public SelectionPath RequiredField { get; init; }
    public ITypeNode Type { get; init; }

    public void Deconstruct(
        out string name,
        out ImmutableArray<OperationPlanNode>? dependsOn,
        out SelectionPath? selectionSet,
        out SelectionPath requiredField,
        out ITypeNode type)
    {
        name = Name;
        dependsOn = DependsOn;
        selectionSet = SelectionSet;
        requiredField = RequiredField;
        type = Type;
    }
}
