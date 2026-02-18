namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Configures the operation planner, including cost-formula tuning
/// and planning guardrails.
/// All guardrail properties default to <c>null</c> (disabled).
/// </summary>
public sealed class OperationPlannerOptions
{
    private bool _isReadOnly;

    /// <summary>
    /// Gets the default (read-only) planner options.
    /// </summary>
    public static OperationPlannerOptions Default { get; } = CreateDefault();

    /// <summary>
    /// Gets or sets the weight applied per sequential execution depth level.
    /// Higher values bias planning toward shallower execution trees.
    /// </summary>
    public double DepthWeight
    {
        get;
        set
        {
            ExpectMutableOptions();
            field = value;
        }
    } = 15.0;

    /// <summary>
    /// Gets or sets the weight applied per operation step.
    /// Higher values bias planning toward fewer total operations.
    /// </summary>
    public double OperationWeight
    {
        get;
        set
        {
            ExpectMutableOptions();
            field = value;
        }
    } = 1.5;

    /// <summary>
    /// Gets or sets the weight applied for each operation beyond the fan-out penalty threshold.
    /// </summary>
    public double ExcessFanoutWeight
    {
        get;
        set
        {
            ExpectMutableOptions();
            field = value;
        }
    } = 3.0;

    /// <summary>
    /// Gets or sets the maximum number of operations allowed at one depth level before
    /// excess fan-out penalties are added.
    /// </summary>
    public int FanoutPenaltyThreshold
    {
        get;
        set
        {
            ExpectMutableOptions();
            field = value;
        }
    } = 8;

    /// <summary>
    /// Gets or sets the maximum allowed planning duration for a single operation.
    /// <c>null</c> by default (no time limit).
    /// </summary>
    public TimeSpan? MaxPlanningTime
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (value is { } maxPlanningTime && maxPlanningTime <= TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "The planner max planning time must be greater than zero.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of planner nodes that may be expanded.
    /// <c>null</c> by default (no node expansion limit).
    /// </summary>
    public int? MaxExpandedNodes
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (value is < 1)
            {
                throw new ArgumentException(
                    "The planner max expanded nodes must be at least 1.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum planner queue size.
    /// <c>null</c> by default (no queue size limit).
    /// </summary>
    public int? MaxQueueSize
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (value is < 1)
            {
                throw new ArgumentException(
                    "The planner max queue size must be at least 1.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of options that may be generated per planner work item.
    /// <c>null</c> by default (no per-work-item limit).
    /// </summary>
    public int? MaxGeneratedOptionsPerWorkItem
    {
        get;
        set
        {
            ExpectMutableOptions();

            if (value is < 1)
            {
                throw new ArgumentException(
                    "The planner max generated options per work item must be at least 1.");
            }

            field = value;
        }
    }

    internal void MakeReadOnly()
        => _isReadOnly = true;

    private void ExpectMutableOptions()
    {
        if (_isReadOnly)
        {
            throw new InvalidOperationException("The options are read-only.");
        }
    }

    private static OperationPlannerOptions CreateDefault()
    {
        var options = new OperationPlannerOptions();
        options.MakeReadOnly();
        return options;
    }
}
