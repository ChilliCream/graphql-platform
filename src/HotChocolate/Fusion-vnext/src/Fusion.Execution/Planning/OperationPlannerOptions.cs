namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Configures the parallelism-aware planner path-cost formula.
/// </summary>
public sealed class OperationPlannerOptions
{
    /// <summary>
    /// Gets the default planner options.
    /// </summary>
    public static OperationPlannerOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the weight applied per sequential execution depth level.
    /// Higher values bias planning toward shallower execution trees.
    /// </summary>
    public double DepthWeight { get; init; } = 15.0;

    /// <summary>
    /// Gets or sets the weight applied per operation step.
    /// Higher values bias planning toward fewer total operations.
    /// </summary>
    public double OperationWeight { get; init; } = 1.5;

    /// <summary>
    /// Gets or sets the weight applied for each operation beyond the fan-out penalty threshold.
    /// </summary>
    public double ExcessFanoutWeight { get; init; } = 3.0;

    /// <summary>
    /// Gets or sets the maximum number of operations allowed at one depth level before
    /// excess fan-out penalties are added.
    /// </summary>
    public int FanoutPenaltyThreshold { get; init; } = 8;
}
