using HotChocolate.CostAnalysis.Directives;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.DescriptorExtensions;

public static class ListSizeObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Applies the <c>@listSize</c> directive. The purpose of the <c>@listSize</c> directive is to
    /// either inform the static analysis about the size of returned lists (if that information is
    /// statically available), or to point the analysis to where to find that information.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="assumedSize">
    /// The maximum length of the list returned by this field.
    /// </param>
    /// <param name="slicingArguments">
    /// The arguments of this field with numeric type that are slicing arguments. Their value
    /// determines the size of the returned list.
    /// </param>
    /// <param name="sizedFields">
    /// The subfield(s) that the list size applies to.
    /// </param>
    /// <param name="requireOneSlicingArgument">
    /// Whether to require a single slicing argument in the query. If that is not the case (i.e., if
    /// none or multiple slicing arguments are present), the static analysis will throw an error.
    /// </param>
    /// <returns>
    /// Returns the object field descriptor for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="descriptor"/> is <c>null</c>.
    /// </exception>
    public static IObjectFieldDescriptor ListSize(
        this IObjectFieldDescriptor descriptor,
        int? assumedSize = null,
        List<string>? slicingArguments = null,
        List<string>? sizedFields = null,
        bool requireOneSlicingArgument = true)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor.Directive(
            new ListSizeDirective(
                assumedSize,
                slicingArguments,
                sizedFields,
                requireOneSlicingArgument));
    }
}
