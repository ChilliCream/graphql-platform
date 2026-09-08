using System.Collections.Immutable;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The compiled plan and per-request estimates a consumer stores on the
/// request context after cost enforcement.
/// </summary>
/// <param name="Plan">
/// The compiled cost plan that was enforced.
/// </param>
/// <param name="Estimates">
/// The evaluated estimate for every coerced variable set of the request.
/// A single-operation request carries exactly one estimate.
/// </param>
/// <param name="IsStaticBound">
/// <see langword="true"/> when <paramref name="Estimates"/> holds the
/// static bound (no coerced variables were evaluated) rather than
/// per-request evaluated estimates.
/// </param>
public sealed record CostAnalysisResult(
    CostPlan Plan,
    ImmutableArray<CostEstimate> Estimates,
    bool IsStaticBound);
