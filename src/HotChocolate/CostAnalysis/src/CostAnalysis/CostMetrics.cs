namespace HotChocolate.CostAnalysis;

/// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-__cost</summary>
public sealed class CostMetrics
{
    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost</summary>
    public double FieldCost { get; set; } = 0;

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Type-Cost</summary>
    public double TypeCost { get; set; } = 0;
}
