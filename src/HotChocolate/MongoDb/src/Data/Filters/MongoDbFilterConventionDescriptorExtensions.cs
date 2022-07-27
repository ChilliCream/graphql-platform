using HotChocolate.Data.Filters;
using HotChocolate.Data.MongoDb.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data;

/// <summary>
/// Common extensions for <see cref="IMongoDbFilterConventionDescriptor"/>
/// </summary>
public static class MongoDbFilterConventionDescriptorExtensions
{
    /// <summary>
    /// Adds a case insensitive contains filter "icontains" to the auto generated list of filters.
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The amended descriptor</returns>
    public static IMongoDbFilterConventionDescriptor AddCaseInsensitiveContains(
        this IMongoDbFilterConventionDescriptor descriptor)
    {
        descriptor
            .Configure<StringOperationFilterInputType>(x => x
                .Operation(DefaultFilterOperations.CaseInsensitiveContains)
                .Type<StringType>());

        descriptor
            .Operation(DefaultFilterOperations.CaseInsensitiveContains)
            .Name("icontains")
            .Description("Contains ignoring case");

        MongoDbFilterProviderExtension extension =
            new(x => x.AddFieldHandler<MongoDbStringCaseInsensitiveContainsHandler>());

        descriptor.AddProviderExtension(extension);

        return descriptor;
    }
}
