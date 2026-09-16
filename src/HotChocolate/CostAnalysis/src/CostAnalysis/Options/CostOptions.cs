namespace HotChocolate.CostAnalysis;

/// <summary>
/// Options for cost analysis.
/// </summary>
public sealed class CostOptions
{
    private bool _skipAnalyzer;
    private bool _enforceCostLimits = true;

    /// <summary>
    /// Gets or sets the maximum allowed field cost.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double MaxFieldCost
    {
        get;
        set
        {
            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxFieldCost), value);
            }

            field = value;
        }
    } = 1_000;

    /// <summary>
    /// Gets or sets the maximum allowed type cost.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double MaxTypeCost
    {
        get;
        set
        {
            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxTypeCost), value);
            }

            field = value;
        }
    } = 1_000;

    /// <summary>
    /// Defines if the analyzer shall enforce cost limits.
    /// </summary>
    public bool EnforceCostLimits
    {
        get => _enforceCostLimits;
        set
        {
            if (value)
            {
                SkipAnalyzer = false;
            }

            _enforceCostLimits = value;
        }
    }

    /// <summary>
    /// Skips the cost analyzer.
    /// </summary>
    public bool SkipAnalyzer
    {
        get => _skipAnalyzer;
        set
        {
            if (value)
            {
                EnforceCostLimits = false;
            }
            _skipAnalyzer = value;
        }
    }

    /// <summary>
    /// Defines if cost defaults shall be applied to the schema.
    /// </summary>
    public bool ApplyCostDefaults { get; set; } = true;

    /// <summary>
    /// Defines if the non-spec slicing argument default value shall be applied.
    /// </summary>
    public bool ApplySlicingArgumentDefaultValue { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cost for an async resolver pipeline.
    /// </summary>
    public double? DefaultResolverCost { get; set; } = 10.0;

    /// <summary>
    /// Gets the cost defaults for filtering.
    /// </summary>
    public FilterCostOptions Filtering { get; } = new();

    /// <summary>
    /// Gets the cost defaults for sorting.
    /// </summary>
    public SortCostOptions Sorting { get; } = new();

    /// <summary>
    /// Gets or sets the assumed size of a list field that has no applicable
    /// <c>@listSize</c> information. <see cref="double.PositiveInfinity"/> by default.
    /// The value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double DefaultListSize
    {
        get;
        set
        {
            if (double.IsNaN(value) || value < 0)
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(DefaultListSize), value);
            }

            field = value;
        }
    } = double.PositiveInfinity;

    /// <summary>
    /// Gets or sets the maximum number of compiled cost plans cached per schema.
    /// <c>256</c> by default.
    /// </summary>
    public int CostPlanCacheSize { get; set; } = 256;

    /// <summary>
    /// Enables the response-size analysis for the schema and sets its default limit.
    /// <c>null</c> (the default) disables the analysis and the metric.
    /// A non-null value must be a non-negative finite number or positive infinity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is NaN, negative, or negative infinity.
    /// </exception>
    public double? MaxResponseSize
    {
        get;
        set
        {
            if (value is { } size && (double.IsNaN(size) || size < 0))
            {
                throw ThrowHelper.InvalidCostOptionValue(nameof(MaxResponseSize), size);
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of exact cases evaluated for a single
    /// operation. <c>null</c> uses the default (510).
    /// </summary>
    public int? CaseBudget { get; set; }

    /// <summary>
    /// Gets or sets the behavior once compiling one operation exhausts
    /// <see cref="CaseBudget"/>. <c>null</c> uses the default
    /// (<see cref="CaseBudgetExceededBehavior.EvaluatePerRequest"/>).
    /// </summary>
    public CaseBudgetExceededBehavior? CaseBudgetExceededBehavior { get; set; }
}
