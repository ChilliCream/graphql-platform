namespace HotChocolate.Fusion.Types;

/// <summary>
/// Defines the cost analysis options declared by a composed schema.
/// All values are <see langword="null"/> when the schema does not declare the corresponding option.
/// </summary>
public sealed record FusionSchemaCostOptions
{
    /// <summary>
    /// Gets an empty set of schema cost options.
    /// </summary>
    public static FusionSchemaCostOptions Empty { get; } = new();

    /// <summary>
    /// Initializes an empty set of schema cost options.
    /// </summary>
    public FusionSchemaCostOptions()
    {
    }

    internal FusionSchemaCostOptions(
        int? defaultListSize,
        double? maxFieldCost,
        double? maxTypeCost,
        bool? enforceCostLimits,
        bool? skipAnalyzer,
        double? maxResponseSize,
        int? costPlanCacheSize,
        int? caseBudget,
        FusionCaseBudgetExceededBehavior? caseBudgetExceededBehavior)
    {
        DefaultListSize = defaultListSize;
        MaxFieldCost = maxFieldCost;
        MaxTypeCost = maxTypeCost;
        EnforceCostLimits = enforceCostLimits;
        SkipAnalyzer = skipAnalyzer;
        MaxResponseSize = maxResponseSize;
        CostPlanCacheSize = costPlanCacheSize;
        CaseBudget = caseBudget;
        CaseBudgetExceededBehavior = caseBudgetExceededBehavior;
    }

    /// <summary>
    /// Gets the assumed size of a list field with no applicable <c>@listSize</c> information.
    /// </summary>
    public int? DefaultListSize { get; }

    /// <summary>
    /// Gets the maximum allowed field cost.
    /// </summary>
    public double? MaxFieldCost { get; }

    /// <summary>
    /// Gets the maximum allowed type cost.
    /// </summary>
    public double? MaxTypeCost { get; }

    /// <summary>
    /// Gets whether cost limits are enforced.
    /// </summary>
    public bool? EnforceCostLimits { get; }

    /// <summary>
    /// Gets whether cost analysis is skipped.
    /// </summary>
    public bool? SkipAnalyzer { get; }

    /// <summary>
    /// Gets the maximum allowed response size.
    /// </summary>
    public double? MaxResponseSize { get; }

    /// <summary>
    /// Gets the capacity of the cost plan cache.
    /// </summary>
    public int? CostPlanCacheSize { get; }

    /// <summary>
    /// Gets the maximum number of cost analysis cases.
    /// </summary>
    public int? CaseBudget { get; }

    /// <summary>
    /// Gets how cost analysis proceeds when the case budget is exhausted.
    /// </summary>
    public FusionCaseBudgetExceededBehavior? CaseBudgetExceededBehavior { get; }
}

/// <summary>
/// Defines how cost analysis proceeds when the schema case budget is exhausted.
/// </summary>
public enum FusionCaseBudgetExceededBehavior
{
    /// <summary>
    /// Evaluates the cost for each request using its variable values.
    /// </summary>
    EvaluatePerRequest,

    /// <summary>
    /// Uses an upper-bound estimate after the case budget is exhausted.
    /// </summary>
    Overestimate
}
