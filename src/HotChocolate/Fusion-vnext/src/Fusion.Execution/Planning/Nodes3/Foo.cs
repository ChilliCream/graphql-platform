using System.Collections.Immutable;
using System.ComponentModel.Design;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning.Nodes3;

public record PlanNode
{
    public PlanNode? Previous { get; init; }

    public required PlanNodeKind Kind { get; init; }

    public required SelectionPath Path { get; init; }

    public required string SchemaName { get; init; }

    public required SelectionSetIndex SelectionSetIndex { get; init; }

    public required ImmutableStack<BacklogItem> Backlog { get; init; }

    public ImmutableList<PlanStep> Steps { get; init; } = ImmutableList<PlanStep>.Empty;

    public Lookup? Lookup { get; init; }

    public double PathCost { get; init; }

    public double BacklogCost { get; init; }

    public double TotalCost => PathCost + BacklogCost;
}

public abstract record PlanStep;

public record OperationPlanStep : PlanStep
{
    public required OperationDefinitionNode Definition { get; init; }

    public required ICompositeNamedType Type { get; init; }

    public required ImmutableHashSet<int> SelectionSets { get; init; }

    public required string SchemaName { get; init; }
}

public enum PlanNodeKind
{
    Root,
    InlineLookupRequirements,
    ResolveLookupRequirements,
    ResolveLookupSelections,
    Complete
}

public sealed record BacklogItem(
    PlanNodeKind Kind,
    SelectionPath Path,
    ISyntaxNode Node,
    SelectionSetNode SelectionSet,
    int SelectionSetId,
    ICompositeNamedType Type);


