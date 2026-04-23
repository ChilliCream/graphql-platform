using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a plan for executing the fields that belong to a specific
/// <c>DeferUsageSet</c>. One <see cref="ExecutionSubPlan"/> is emitted per
/// unique non-empty active defer usage set in the operation. Its data is
/// delivered to every <see cref="DeferUsage"/> in <see cref="DeliveryGroups"/>
/// when the subplan completes.
/// </summary>
public sealed class ExecutionSubPlan
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExecutionSubPlan"/>.
    /// </summary>
    /// <param name="operation">
    /// The compiled operation for this subplan's result mapping.
    /// </param>
    /// <param name="rootNodes">
    /// The root execution nodes that serve as entry points for this subplan.
    /// </param>
    /// <param name="allNodes">
    /// All execution nodes belonging to this subplan.
    /// </param>
    /// <param name="deliveryGroups">
    /// The <see cref="DeferUsage"/> set that keys this subplan, sorted ascending
    /// by <see cref="DeferUsage.Id"/>. Every element is a delivery group that
    /// receives this subplan's data on the wire when the subplan completes.
    /// </param>
    /// <param name="requirements">
    /// The plan-scope requirements that must be supplied from the parent plan
    /// before this subplan can execute. Each requirement maps a variable name
    /// to a selection in the parent plan's result tree.
    /// </param>
    public ExecutionSubPlan(
        Operation operation,
        ImmutableArray<ExecutionNode> rootNodes,
        ImmutableArray<ExecutionNode> allNodes,
        ImmutableArray<DeferUsage> deliveryGroups,
        ImmutableArray<OperationRequirement> requirements)
    {
        Operation = operation;
        RootNodes = rootNodes;
        AllNodes = allNodes;
        DeliveryGroups = deliveryGroups;
        Requirements = requirements.IsDefault ? [] : requirements;
    }

    /// <summary>
    /// Gets the compiled operation for this subplan.
    /// This is a standalone operation compiled from the rewritten subplan AST,
    /// used for result mapping during execution.
    /// </summary>
    public Operation Operation { get; }

    /// <summary>
    /// Gets the root execution nodes that serve as entry points for this subplan.
    /// </summary>
    public ImmutableArray<ExecutionNode> RootNodes { get; }

    /// <summary>
    /// Gets all execution nodes belonging to this subplan.
    /// </summary>
    public ImmutableArray<ExecutionNode> AllNodes { get; }

    /// <summary>
    /// Gets the <see cref="DeferUsage"/> set that keys this subplan, sorted
    /// ascending by <see cref="DeferUsage.Id"/>. When this subplan completes,
    /// every <see cref="DeferUsage"/> in this set receives the subplan's data
    /// as an incremental payload on the wire.
    /// </summary>
    public ImmutableArray<DeferUsage> DeliveryGroups { get; }

    /// <summary>
    /// Gets the plan-scope requirements that the parent plan must supply
    /// before this subplan can execute. Each requirement wires a variable
    /// used inside this subplan to a selection in the parent plan's result
    /// tree.
    /// </summary>
    public ImmutableArray<OperationRequirement> Requirements { get; }

    /// <summary>
    /// Gets the <see cref="ExecutionNode.Id"/> in the owning plan (the main plan
    /// for top-level subplans, the parent subplan's plan for nested subplans)
    /// whose fetch resolves the selection set where this subplan was anchored.
    /// Always populated for a sealed plan; query plan visualizers can use this
    /// to attach the subplan to the node that produces its enclosing data.
    /// Set during plan construction.
    /// </summary>
    public int ParentNodeId { get; internal set; }
}
