using System.Collections.Immutable;
using System.ComponentModel.Design;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public abstract record PlanNode
{
    public required int Id { get; init; }

    public PlanNode? Previous { get; init; }

    public abstract ISyntaxNode SyntaxNode { get; }

    public required SelectionPath Path { get; init; }

    public required string SchemaName { get; init; }

    public required string NextSchemaName { get; init; }

    public double PathCost { get; init; }

    public double BacklogCost { get; init; }

    public required ImmutableQueue<BacklogItem> Backlog { get; init; }

    public double TotalCost => PathCost + BacklogCost;

    // public required ImmutableDictionary<int, ImmutableList<AvailableField>> Fields { get; init; }
}

public record OperationPlanNode : PlanNode
{
    public required SelectionSetNode SelectionSet { get; init; }

    public required ICompositeNamedType Type { get; init; }

    public override ISyntaxNode SyntaxNode => SelectionSet;
}

public sealed record BacklogItem(
    SelectionPath Path,
    ISyntaxNode Node,
    int SelectionSetId,
    SelectionSetNode SelectionSet,
    ICompositeNamedType Type);

public enum BacklogItemKind
{
    Selections,
    Requirements
}
