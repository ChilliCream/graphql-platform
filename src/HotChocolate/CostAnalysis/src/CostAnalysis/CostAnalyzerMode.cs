namespace HotChocolate.CostAnalysis;

/// <summary>
/// Defines the cost analyzer mode.
/// </summary>
[Flags]
internal enum CostAnalyzerMode
{
    /// <summary>
    /// Only analyzes the operation and stores the operation cost metrics in the request context.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Only analyzes the operation and stores the operation cost metrics in the request context.
    /// </summary>
    Analyze = 1,

    /// <summary>
    /// Enforces the defined cost limits but does not report any metrics in the response.
    /// </summary>
    Report = 2,

    /// <summary>
    /// Enforces the defined cost limits but does not report any metrics in the response.
    /// </summary>
    Enforce = 4,

    /// <summary>
    /// Execute the request after analyzing the operation cost.
    /// </summary>
    Execute = 8,
}
