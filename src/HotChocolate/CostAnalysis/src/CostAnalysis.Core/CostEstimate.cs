namespace HotChocolate.CostAnalysis;

/// <summary>
/// The result of evaluating a <see cref="CostPlan"/>.
/// </summary>
/// <param name="FieldCost">
/// The accumulated field cost.
/// </param>
/// <param name="TypeCost">
/// The accumulated type cost.
/// </param>
/// <param name="MaxResponseSize">
/// The maximum response size, or <see langword="null"/> when
/// <see cref="CostAnalyses.ResponseSize"/> was not evaluated.
/// </param>
public readonly record struct CostEstimate(double FieldCost, double TypeCost, double? MaxResponseSize);
