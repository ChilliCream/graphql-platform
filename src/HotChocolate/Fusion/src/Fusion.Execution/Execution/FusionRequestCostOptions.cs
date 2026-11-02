using HotChocolate.Execution;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Request options for cost analysis, attached to a single request through
/// <see cref="OperationRequestBuilder"/> or <see cref="RequestContext"/>.
/// A value set here replaces the corresponding <see cref="FusionCostOptions"/> value for that request.
/// </summary>
public sealed record FusionRequestCostOptions
{
    private readonly double _maxFieldCost;
    private readonly double _maxTypeCost;
    private readonly double? _maxResponseSize;

    /// <summary>
    /// Request options for cost analysis.
    /// </summary>
    /// <param name="maxFieldCost">
    /// The maximum allowed field cost.
    /// </param>
    /// <param name="maxTypeCost">
    /// The maximum allowed type cost.
    /// </param>
    /// <param name="enforceCostLimits">
    /// Whether to enforce cost limits.
    /// </param>
    /// <param name="skipAnalyzer">
    /// Whether to skip cost analysis.
    /// </param>
    /// <param name="maxResponseSize">
    /// The response-size limit for this request, or <see langword="null"/> when this request
    /// does not enforce a response-size limit.
    /// </param>
    public FusionRequestCostOptions(
        double maxFieldCost,
        double maxTypeCost,
        bool enforceCostLimits,
        bool skipAnalyzer,
        double? maxResponseSize)
    {
        MaxFieldCost = maxFieldCost;
        MaxTypeCost = maxTypeCost;
        EnforceCostLimits = enforceCostLimits;
        SkipAnalyzer = skipAnalyzer;
        MaxResponseSize = maxResponseSize;
    }

    /// <summary>
    /// Gets the maximum allowed field cost.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double MaxFieldCost
    {
        get => _maxFieldCost;
        init
        {
            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxFieldCost), value);
            }

            _maxFieldCost = value;
        }
    }

    /// <summary>
    /// Gets the maximum allowed type cost.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double MaxTypeCost
    {
        get => _maxTypeCost;
        init
        {
            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxTypeCost), value);
            }

            _maxTypeCost = value;
        }
    }

    /// <summary>
    /// Gets whether to enforce field-cost, type-cost, and response-size limits for this request.
    /// </summary>
    public bool EnforceCostLimits { get; init; }

    /// <summary>
    /// Gets whether the cost analyzer is skipped entirely for this request.
    /// </summary>
    public bool SkipAnalyzer { get; init; }

    /// <summary>
    /// Gets the response-size limit for this request, or <see langword="null"/> when this
    /// request does not enforce a response-size limit.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double? MaxResponseSize
    {
        get => _maxResponseSize;
        init
        {
            if (value is { } size && (double.IsNaN(size) || size < 0))
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxResponseSize), size);
            }

            _maxResponseSize = value;
        }
    }
}
