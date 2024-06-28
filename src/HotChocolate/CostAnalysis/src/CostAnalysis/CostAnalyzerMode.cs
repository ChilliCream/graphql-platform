namespace HotChocolate.CostAnalysis;

/// <summary>
/// Defines the cost analyzer mode.
/// </summary>
internal enum CostAnalyzerMode
{
    /// <summary>
    /// Only analyzes the operation and stores the operation cost metrics in the request context.
    /// </summary>
    Analysis = 0,

    /// <summary>
    /// Enforces the defined cost limits but does not report any metrics in the response.
    /// </summary>
    Enforce = 1,

    /// <summary>
    /// Enforces the defined cost limits and reports the operation cost metrics in the response.
    /// </summary>
    EnforceAndReport = 2,

    /// <summary>
    /// Validates the operation and reports the operation cost metrics but does not execute the operation.
    /// </summary>
    ValidateAndReport = 3
}
