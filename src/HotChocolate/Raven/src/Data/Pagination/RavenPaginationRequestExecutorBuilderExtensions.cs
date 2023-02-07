using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace HotChocolate.Data.Raven.Pagination;

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
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddCursorPagingProvider<RavenCursorPagingProvider>(providerName, defaultProvider);

        builder.AddOffsetPagingProvider<RavenOffsetPagingProvider>(providerName, defaultProvider);

        return builder;
    }
}
