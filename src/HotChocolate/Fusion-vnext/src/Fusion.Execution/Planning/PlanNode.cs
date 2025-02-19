using System.Collections.Immutable;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public record PlanNode
{
    public PlanNode? Previous { get; init; }

    public required string SchemaName { get; init; }

    public required ISelectionSetIndex SelectionSetIndex { get; init; }

    public required ImmutableStack<WorkItem> Backlog { get; init; }

    public ImmutableList<PlanStep> Steps { get; init; } = ImmutableList<PlanStep>.Empty;

    public double PathCost { get; init; }

    public double BacklogCost { get; init; }

    public double TotalCost => PathCost + BacklogCost;
}
