using HotChocolate.Data;
using HotChocolate.Data.Raven.Pagination;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IRequestExecutorBuilder"/> for RavenDb pagination
/// </summary>
public static class RavenPaginationRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds the Raven cursor and offset paging providers.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="providerName">
    /// The name which shall be used to refer to this registration.
    /// </param>
    /// <param name="defaultProvider">
    /// Defines if these providers shall be registered as default providers.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for further configuration chaining.
    /// </returns>
    public static IRequestExecutorBuilder AddRavenPagingProviders(
        this IRequestExecutorBuilder builder,
        string providerName = RavenPagination.ProviderName,
        bool defaultProvider = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RegisterDocumentStore();
        builder.AddCursorPagingProvider<RavenCursorPagingProvider>(providerName, defaultProvider);
        builder.AddOffsetPagingProvider<RavenOffsetPagingProvider>(providerName, defaultProvider);

        return builder;
    }
}
