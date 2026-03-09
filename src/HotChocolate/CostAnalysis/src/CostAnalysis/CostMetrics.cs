namespace HotChocolate.CostAnalysis;

/// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-__cost</summary>
public sealed record CostMetrics
{
    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost</summary>
    public double FieldCost { get; init; }

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Type-Cost</summary>
    public double TypeCost { get; init; }
}
