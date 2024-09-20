using System.Collections.Immutable;

namespace HotChocolate.CostAnalysis.Types;

/// <summary>
/// The purpose of the <c>@listSize</c> directive is to either inform the static analysis about the
/// size of returned lists (if that information is statically available), or to point the analysis
/// to where to find that information.
/// </summary>
/// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-The-List-Size-Directive">
/// Specification URL
/// </seealso>
public sealed class ListSizeDirective
{
    /// <summary>
    /// The purpose of the <c>@listSize</c> directive is to either inform the static analysis about the
    /// size of returned lists (if that information is statically available), or to point the analysis
    /// to where to find that information.
    /// </summary>
    /// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-The-List-Size-Directive">
    /// Specification URL
    /// </seealso>
    public ListSizeDirective(int? assumedSize = null,
        ImmutableArray<string>? slicingArguments = null,
        ImmutableArray<string>? sizedFields = null,
        bool? requireOneSlicingArgument = null)
    {
        AssumedSize = assumedSize;
        SlicingArguments = slicingArguments ?? ImmutableArray<string>.Empty;
        SizedFields = sizedFields ?? ImmutableArray<string>.Empty;

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-requireOneSlicingArgument
        // Per default, requireOneSlicingArgument is enabled,
        // and has to be explicitly disabled if not desired for a field.
        RequireOneSlicingArgument = SlicingArguments is { Length: > 0 } && (requireOneSlicingArgument ?? true);
    }

    /// <summary>
    /// The <c>assumedSize</c> argument can be used to statically define the maximum length of a
    /// list returned by a field.
    /// </summary>
    /// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-assumedSize">
    /// Specification URL
    /// </seealso>
    public int? AssumedSize { get; }

    /// <summary>
    /// The <c>slicingArguments</c> argument can be used to define which of the field's arguments
    /// with numeric type are slicing arguments, so that their value determines the size of the list
    /// returned by that field. It may specify a list of multiple slicing arguments.
    /// </summary>
    /// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-slicingArguments">
    /// Specification URL
    /// </seealso>
    public ImmutableArray<string> SlicingArguments { get; }

    /// <summary>
    /// The <c>sizedFields</c> argument can be used to define that the value of the
    /// <c>assumedSize</c> argument or of a slicing argument does not affect the size of a list
    /// returned by a field itself, but that of a list returned by one of its sub-fields.
    /// </summary>
    /// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-sizedFields">
    /// Specification URL
    /// </seealso>
    public ImmutableArray<string> SizedFields { get; }

    /// <summary>
    /// The <c>requireOneSlicingArgument</c> argument can be used to inform the static analysis that
    /// it should expect that exactly one of the defined slicing arguments is present in a query. If
    /// that is not the case (i.e., if none or multiple slicing arguments are present), the static
    /// analysis will throw an error.
    /// </summary>
    /// <seealso href="https://ibm.github.io/graphql-specs/cost-spec.html#sec-requireOneSlicingArgument">
    /// Specification URL
    /// </seealso>
    public bool RequireOneSlicingArgument { get; }
}
