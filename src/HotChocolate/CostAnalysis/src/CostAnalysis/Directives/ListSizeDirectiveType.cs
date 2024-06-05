using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Directives;

/// <summary>
/// The purpose of the <c>@listSize</c> directive is to either inform the static analysis about the
/// size of returned lists (if that information is statically available), or to point the analysis
/// to where to find that information.
/// </summary>
/// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-The-List-Size-Directive">
/// Specification URL
/// </seealso>
public sealed class ListSizeDirectiveType : DirectiveType<ListSizeDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<ListSizeDirective> descriptor)
    {
        descriptor
            .Name("listSize")
            .Description(
                "The purpose of the `@listSize` directive is to either inform the static " +
                "analysis about the size of returned lists (if that information is statically " +
                "available), or to point the analysis to where to find that information.")
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(t => t.AssumedSize)
            .Description(
                "The `assumedSize` argument can be used to statically define the maximum length " +
                "of a list returned by a field.");

        descriptor
            .Argument(t => t.SlicingArguments)
            .Description(
                "The `slicingArguments` argument can be used to define which of the field's " +
                "arguments with numeric type are slicing arguments, so that their value " +
                "determines the size of the list returned by that field. It may specify a list " +
                "of multiple slicing arguments.");

        descriptor
            .Argument(t => t.SizedFields)
            .Description(
                "The `sizedFields` argument can be used to define that the value of the " +
                "`assumedSize` argument or of a slicing argument does not affect the size of a " +
                "list returned by a field itself, but that of a list returned by one of its " +
                "sub-fields.");

        descriptor
            .Argument(t => t.RequireOneSlicingArgument)
            .DefaultValue(true)
            .Description(
                "The `requireOneSlicingArgument` argument can be used to inform the static " +
                "analysis that it should expect that exactly one of the defined slicing " +
                "arguments is present in a query. If that is not the case (i.e., if none or " +
                "multiple slicing arguments are present), the static analysis may throw an error.");
    }
}
