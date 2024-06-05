namespace HotChocolate.CostAnalysis;

/// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-__cost</summary>
internal sealed class CostMetrics
{
    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Counts</summary>
    public Dictionary<string, int> FieldCounts { get; } = [];

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Type-Counts</summary>
    public Dictionary<string, int> TypeCounts { get; } = [];

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Input-Type-Counts</summary>
    public Dictionary<string, int> InputTypeCounts { get; } = [];

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Input-Field-Counts</summary>
    public Dictionary<string, int> InputFieldCounts { get; } = [];

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Argument-Counts</summary>
    public Dictionary<string, int> ArgumentCounts { get; } = [];

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Directive-Counts</summary>
    public Dictionary<string, int> DirectiveCounts { get; } = [];

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost</summary>
    public double FieldCost { get; set; } = 0;

    /// <summary>https://ibm.github.io/graphql-specs/cost-spec.html#sec-Type-Cost</summary>
    public double TypeCost { get; set; } = 0;

    public Dictionary<string, double> FieldCostByLocation { get; } = [];

    public Dictionary<string, double> TypeCostByLocation { get; } = [];
}
