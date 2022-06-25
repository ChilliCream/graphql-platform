using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions for <see cref="IQueryableFilterConventionDescriptor"/>
/// </summary>
public static class QueryableFilterConventionDescriptorExtensions
{
    /// <summary>
    /// Adds a case insensitive contains filter "icontains" to the auto generated list of filters.
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The amended descriptor</returns>
    public static IQueryableFilterConventionDescriptor AddCaseInsensitiveContains(
        this IQueryableFilterConventionDescriptor descriptor)
    {
        descriptor
            .Configure<StringOperationFilterInputType>(
                x => x.Operation(DefaultFilterOperations.CaseInsensitiveContains)
                    .Type<StringType>());

        descriptor
            .Operation(DefaultFilterOperations.CaseInsensitiveContains)
            .Name("icontains")
            .Description("Contains ignoring case");

        QueryableFilterProviderExtension extension =
            new(x => x.AddFieldHandler<QueryableStringCaseInsensitiveContainsHandler>());

        descriptor.AddProviderExtension(extension);

        return descriptor;
    }
}
