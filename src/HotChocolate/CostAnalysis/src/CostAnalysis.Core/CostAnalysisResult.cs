using System.Collections.Immutable;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A compiled cost plan and the estimates for a request.
/// </summary>
/// <param name="Plan">
/// The compiled cost plan that was enforced.
/// </param>
/// <param name="Estimates">
/// The evaluated estimate for every coerced variable set of the request.
/// A single-operation request carries exactly one estimate.
/// </param>
public sealed record CostAnalysisResult(
    CostPlan Plan,
    ImmutableArray<CostEstimate> Estimates);
