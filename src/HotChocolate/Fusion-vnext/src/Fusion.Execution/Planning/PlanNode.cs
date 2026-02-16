using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A plan node represents one possible planning step to create a plan for an operation.
/// The <see cref="Backlog"/> represents the pieces of work that are still to be planned
/// to have a complete execution plan for the <see cref="OperationDefinition"/>.
/// </summary>
internal sealed record PlanNode
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
    /// Gets the first 8 characters of the hash of the original operation document.
    /// </summary>
    public required string ShortHash { get; init; }

    /// <summary>
    /// The source schema against which the next <see cref="WorkItem"/> is planned.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Planner cost options used by this node.
    /// </summary>
    public required OperationPlannerOptions Options { get; init; }

    /// <summary>
    /// The index of the selection set.
    /// </summary>
    public required ISelectionSetIndex SelectionSetIndex
    {
        get;
        init
        {
            if (value is SelectionSetIndexBuilder builder)
            {
                field = builder.Build();
            }
            else
            {
                field = value;
            }
        }
    }

    public required ImmutableStack<WorkItem> Backlog { get; init; }

    /// <summary>
    /// Incremental backlog projection state for optimistic lower-bound estimation.
    /// </summary>
    public BacklogCostState BacklogCostState { get; init; } = BacklogCostState.Empty;

    /// <summary>
    /// The optimistic lower bound for all work currently in <see cref="Backlog"/>.
    /// This includes operation, remaining-depth, and projected excess-fanout components.
    /// It is updated incrementally by planner transitions.
    /// </summary>
    public double BacklogLowerBound { get; init; }

    public ImmutableList<PlanStep> Steps { get; init; } = [];

    /// <summary>
    /// The number of <see cref="OperationPlanStep"/> instances in <see cref="Steps"/>.
    /// This is updated incrementally by planner transitions.
    /// </summary>
    public int OperationStepCount { get; init; }

    /// <summary>
    /// Maximum operation depth seen so far for this node.
    /// </summary>
    public int MaxDepth { get; init; }

    /// <summary>
    /// Cumulative excess fan-out across all depth levels.
    /// </summary>
    public int ExcessFanout { get; init; }

    /// <summary>
    /// Number of operation steps per depth level.
    /// </summary>
    public ImmutableDictionary<int, int> OpsPerLevel { get; init; } = ImmutableDictionary<int, int>.Empty;

    /// <summary>
    /// Depth lookup for operation step ids.
    /// </summary>
    public ImmutableDictionary<int, int> OperationStepDepths { get; init; }
        = ImmutableDictionary<int, int>.Empty;

    public uint LastRequirementId { get; init; }

    public double PathCost
        => MaxDepth * Options.DepthWeight
            + OperationStepCount * Options.OperationWeight
            + ExcessFanout * Options.ExcessFanoutWeight;

    public double BacklogCost => BacklogLowerBound;

    public double ResolutionCost { get; init; }

    public double TotalCost => PathCost + BacklogLowerBound + ResolutionCost;

    public string CreateOperationName(int stepId)
    {
        if (OperationDefinition.Name is null)
        {
            return $"Op_{ShortHash}_{stepId}";
        }

        return $"{OperationDefinition.Name.Value}_{ShortHash}_{stepId}";
    }
}
