using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A plan node represents one possible planning step to create a plan for an operation.
/// The <see cref="Backlog"/> represents the pieces of work that are still to be planned
/// to have a complete execution plan for the <see cref="OperationDefinition"/>.
/// </summary>
public sealed record PlanNode
{
    /// <summary>
    /// The previous plan node.
    /// </summary>
    public PlanNode? Previous { get; init; }

    /// <summary>
    /// The original operation definitions that is being planned.
    /// </summary>
    public required OperationDefinitionNode OperationDefinition { get; init; }

    /// <summary>
    /// Represents the internal operation definition that includes requirement data
    /// and is used to build requirements variables and the result tree.
    /// </summary>
    public required OperationDefinitionNode InternalOperationDefinition { get; init; }

    /// <summary>
    /// The source schema against which the next <see cref="WorkItem"/> is planned.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// The index of the selection set.
    /// </summary>
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
