namespace HotChocolate.CostAnalysis;

/// <summary>
/// Request options for cost analysis.
/// </summary>
public record RequestCostOptions
{
    private bool _enforceCostLimits;
    private bool _skipAnalyzer;
    private int? _filterVariableMultiplier;

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
    /// Defines if the analyzer shall enforce cost limits.
    /// </param>
    /// <param name="filterVariableMultiplier">
    /// The filter variable multiplier.
    /// </param>
    [Obsolete(
        "The filter variable multiplier is retired. Use the constructor that takes "
        + "skipAnalyzer and maxResponseSize instead.",
        error: true)]
    public RequestCostOptions(
        double maxFieldCost,
        double maxTypeCost,
        bool enforceCostLimits,
        int? filterVariableMultiplier)
    {
        MaxFieldCost = maxFieldCost;
        MaxTypeCost = maxTypeCost;
        EnforceCostLimits = enforceCostLimits;
        FilterVariableMultiplier = filterVariableMultiplier;
    }

    /// <summary>
    /// Gets the maximum allowed field cost.
    /// </summary>
    /// <param name="maxFieldCost">
    /// The maximum allowed field cost.
    /// </param>
    /// <param name="maxTypeCost">
    /// The maximum allowed type cost.
    /// </param>
    /// <param name="enforceCostLimits">
    /// Defines if the analyzer shall enforce cost limits.
    /// </param>
    /// <param name="skipAnalyzer">
    /// Defines if the cost analyzer shall be skipped.
    /// </param>
    /// <param name="filterVariableMultiplier">
    /// The filter variable multiplier.
    /// </param>
    [Obsolete(
        "The filter variable multiplier is retired. Use the constructor that takes "
        + "maxResponseSize instead.",
        error: true)]
    public RequestCostOptions(
        double maxFieldCost,
        double maxTypeCost,
        bool enforceCostLimits,
        bool skipAnalyzer,
        int? filterVariableMultiplier)
    {
        MaxFieldCost = maxFieldCost;
        MaxTypeCost = maxTypeCost;
        EnforceCostLimits = enforceCostLimits;
        SkipAnalyzer = skipAnalyzer;
        FilterVariableMultiplier = filterVariableMultiplier;
    }

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
    /// Defines if the analyzer shall enforce cost limits.
    /// </param>
    /// <param name="skipAnalyzer">
    /// Defines if the cost analyzer shall be skipped.
    /// </param>
    /// <param name="maxResponseSize">
    /// The maximum allowed response size.
    /// </param>
    public RequestCostOptions(
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
    /// </summary>
    public double MaxFieldCost { get; init; }

    /// <summary>
    /// Gets the maximum allowed type cost.
    /// </summary>
    public double MaxTypeCost { get; init; }

    /// <summary>
    /// Defines if the analyzer shall enforce cost limits.
    /// </summary>
    public bool EnforceCostLimits
    {
        get => _enforceCostLimits;
        init
        {
            if (value)
            {
                SkipAnalyzer = false;
            }

            _enforceCostLimits = value;
        }
    }

    /// <summary>
    /// Defines if the cost analyzer shall be skipped.
    /// </summary>
    public bool SkipAnalyzer
    {
        get => _skipAnalyzer;
        init
        {
            if (value)
            {
                EnforceCostLimits = false;
            }

            _skipAnalyzer = value;
        }
    }

    /// <summary>
    /// Gets the maximum allowed response size. <c>null</c> disables the check.
    /// </summary>
    public double? MaxResponseSize { get; init; }

    /// <summary>
    /// Gets the filter variable multiplier.
    /// </summary>
    [Obsolete(
        "The variable multiplier is retired. The cost analyzer evaluates coerced "
        + "variable values directly and no longer needs a compensating multiplier.",
        error: true)]
    public int? FilterVariableMultiplier
    {
        get => _filterVariableMultiplier;
        init => _filterVariableMultiplier = value;
    }

    /// <summary>
    /// Gets the filter variable multiplier for the legacy cost analyzer.
    /// Always <c>null</c> once callers can no longer set <see cref="FilterVariableMultiplier"/>.
    /// </summary>
    internal int? LegacyFilterVariableMultiplier => _filterVariableMultiplier;

    /// <summary>
    /// Deconstructs the request options.
    /// </summary>
    /// <param name="maxFieldCost">
    /// The maximum allowed field cost.
    /// </param>
    /// <param name="maxTypeCost">
    /// The maximum allowed type cost.
    /// </param>
    /// <param name="enforceCostLimits">
    /// Defines if the analyzer shall enforce cost limits.
    /// </param>
    /// <param name="skipAnalyzer">
    /// Defines if the cost analyzer shall be skipped.
    /// </param>
    /// <param name="filterVariableMultiplier">
    /// The filter variable multiplier.
    /// </param>
    [Obsolete(
        "The filter variable multiplier is retired. Use the Deconstruct overload that "
        + "returns maxResponseSize instead.",
        error: true)]
    public void Deconstruct(
        out double maxFieldCost,
        out double maxTypeCost,
        out bool enforceCostLimits,
        out bool skipAnalyzer,
        out int? filterVariableMultiplier)
    {
        maxFieldCost = MaxFieldCost;
        maxTypeCost = MaxTypeCost;
        enforceCostLimits = EnforceCostLimits;
        skipAnalyzer = SkipAnalyzer;
        filterVariableMultiplier = FilterVariableMultiplier;
    }

    /// <summary>
    /// Deconstructs the request options.
    /// </summary>
    /// <param name="maxFieldCost">
    /// The maximum allowed field cost.
    /// </param>
    /// <param name="maxTypeCost">
    /// The maximum allowed type cost.
    /// </param>
    /// <param name="enforceCostLimits">
    /// Defines if the analyzer shall enforce cost limits.
    /// </param>
    /// <param name="skipAnalyzer">
    /// Defines if the cost analyzer shall be skipped.
    /// </param>
    /// <param name="maxResponseSize">
    /// The maximum allowed response size.
    /// </param>
    public void Deconstruct(
        out double maxFieldCost,
        out double maxTypeCost,
        out bool enforceCostLimits,
        out bool skipAnalyzer,
        out double? maxResponseSize)
    {
        maxFieldCost = MaxFieldCost;
        maxTypeCost = MaxTypeCost;
        enforceCostLimits = EnforceCostLimits;
        skipAnalyzer = SkipAnalyzer;
        maxResponseSize = MaxResponseSize;
    }
}
