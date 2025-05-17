using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed record PlanNode
{
    public PlanNode? Previous { get; init; }

    public required OperationDefinitionNode OperationDefinition { get; init; }

    public required string SchemaName { get; init; }

    public required ISelectionSetIndex SelectionSetIndex { get; init; }

    public required ImmutableStack<WorkItem> Backlog { get; init; }

    public ImmutableList<PlanStep> Steps { get; init; } = [];

    public uint LastRequirementId { get; init; }

    public double PathCost { get; init; }

    public double BacklogCost { get; init; }

    public double TotalCost => PathCost + BacklogCost;

    public string? CreateOperationName(int stepId)
    {
        if (OperationDefinition.Name is null)
        {
            return null;
        }

        return $"{OperationDefinition.Name.Value}_{stepId}";
    }
}
