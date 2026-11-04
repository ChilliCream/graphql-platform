namespace HotChocolate.Fusion.Types;

/// <summary>
/// Defines the planning options declared by a composed schema.
/// </summary>
public sealed record PlannerOptions
{
    /// <summary>
    /// Gets the default planning options.
    /// </summary>
    public static PlannerOptions Default { get; } = new();

    /// <summary>
    /// Initializes the default planning options.
    /// </summary>
    public PlannerOptions()
    {
    }

    internal PlannerOptions(
        NodeResolution nodeResolution,
        ShareableFieldRuntimeTypeRouting routing)
    {
        NodeResolution = nodeResolution;
        ShareableFieldRuntimeTypeRouting = routing;
    }

    /// <summary>
    /// Gets how the gateway resolves the <c>Query.node</c> field.
    /// </summary>
    public NodeResolution NodeResolution { get; } = NodeResolution.Gateway;

    /// <summary>
    /// Gets how runtime types are routed for shareable fields whose result type is abstract.
    /// </summary>
    public ShareableFieldRuntimeTypeRouting ShareableFieldRuntimeTypeRouting { get; } = ShareableFieldRuntimeTypeRouting.SourceLocal;

    /// <summary>
    /// Gets the weight applied per sequential execution depth level, or <c>null</c> when not configured.
    /// </summary>
    public double? DepthWeight { get; init; }

    /// <summary>
    /// Gets the weight applied per operation step, or <c>null</c> when not configured.
    /// </summary>
    public double? OperationWeight { get; init; }

    /// <summary>
    /// Gets whether independent operations can be grouped into transport batches, or <c>null</c> when not configured.
    /// </summary>
    public bool? EnableRequestGrouping { get; init; }

    /// <summary>
    /// Gets whether defer support is enabled, or <c>null</c> when not configured.
    /// </summary>
    public bool? EnableDefer { get; init; }

    /// <summary>
    /// Gets whether nested unlabeled defer fragments are inlined, or <c>null</c> when not configured.
    /// </summary>
    public bool? InlineUnlabeledDeferFragments { get; init; }

    /// <summary>
    /// Gets the policy for merging structurally identical lookups, or <c>null</c> when not configured.
    /// </summary>
    public FusionOperationMergePolicy? MergePolicy { get; init; }

    /// <summary>
    /// Gets whether lookups are split to enable entity reuse, or <c>null</c> when not configured.
    /// </summary>
    public bool? EnableEntityReuseSplitting { get; init; }

    /// <summary>
    /// Gets the weight applied for operations beyond the fan-out threshold, or <c>null</c> when not configured.
    /// </summary>
    public double? ExcessFanoutWeight { get; init; }

    /// <summary>
    /// Gets the number of operations at one depth before applying the fan-out penalty, or <c>null</c> when not configured.
    /// </summary>
    public int? FanoutPenaltyThreshold { get; init; }

    /// <summary>
    /// Gets the maximum planning duration for an operation, or <c>null</c> when not configured.
    /// </summary>
    public TimeSpan? MaxPlanningTime { get; init; }

    /// <summary>
    /// Gets the maximum number of planner nodes that may be expanded, or <c>null</c> when not configured.
    /// </summary>
    public int? MaxExpandedNodes { get; init; }

    /// <summary>
    /// Gets the maximum planner queue size, or <c>null</c> when not configured.
    /// </summary>
    public int? MaxQueueSize { get; init; }

    /// <summary>
    /// Gets the maximum number of options generated per planner work item, or <c>null</c> when not configured.
    /// </summary>
    public int? MaxGeneratedOptionsPerWorkItem { get; init; }
}
