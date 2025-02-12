namespace HotChocolate.CostAnalysis;

/// <summary>
/// Request options for cost analysis.
/// </summary>
public record RequestCostOptions
{
    private bool _enforceCostLimits;
    private bool _skipAnalyzer;

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
    /// Gets the filter variable multiplier.
    /// </summary>
    public int? FilterVariableMultiplier { get; init; }

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
}
