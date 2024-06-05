namespace HotChocolate.CostAnalysis;

/// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-__cost</summary>
internal sealed class Cost(CostMetrics requestCosts)
{
    public CostMetrics RequestCosts { get; } = requestCosts;
}
