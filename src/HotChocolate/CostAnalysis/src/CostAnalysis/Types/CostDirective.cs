namespace HotChocolate.CostAnalysis.Types;

/// <summary>
/// The purpose of the <c>cost</c> directive is to define a <c>weight</c> for GraphQL types, fields,
/// and arguments. Static analysis can use these weights when calculating the overall cost of a
/// query or response.
/// </summary>
/// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-The-Cost-Directive">
/// Specification URL
/// </seealso>
public sealed class CostDirective(double weight)
{
    /// <summary>
    /// The <c>weight</c> argument defines what value to add to the overall cost for every
    /// appearance, or possible appearance, of a type, field, argument, etc.
    /// </summary>
    /// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight">
    /// Specification URL
    /// </seealso>
    public double Weight { get; } = weight;
}
