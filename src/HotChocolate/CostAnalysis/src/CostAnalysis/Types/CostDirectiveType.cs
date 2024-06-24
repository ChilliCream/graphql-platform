using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Types;

/// <summary>
/// The purpose of the <c>cost</c> directive is to define a <c>weight</c> for GraphQL types, fields,
/// and arguments. Static analysis can use these weights when calculating the overall cost of a
/// query or response.
/// </summary>
/// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-The-Cost-Directive">
/// Specification URL
/// </seealso>
public sealed class CostDirectiveType : DirectiveType<CostDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<CostDirective> descriptor)
    {
        descriptor
            .Name("cost")
            .Description(
                "The purpose of the `cost` directive is to define a `weight` for GraphQL types, " +
                "fields, and arguments. Static analysis can use these weights when calculating " +
                "the overall cost of a query or response.")
            .Location(
                DirectiveLocation.ArgumentDefinition |
                DirectiveLocation.Enum |
                DirectiveLocation.FieldDefinition |
                DirectiveLocation.InputFieldDefinition |
                DirectiveLocation.Object |
                DirectiveLocation.Scalar);

        descriptor
            .Argument(t => t.Weight)
            .Type<NonNullType<StringType>>()
            .Description(
                "The `weight` argument defines what value to add to the overall cost for every " +
                "appearance, or possible appearance, of a type, field, argument, etc.");
    }
}
