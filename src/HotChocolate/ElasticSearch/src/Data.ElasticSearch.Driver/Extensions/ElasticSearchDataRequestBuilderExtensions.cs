using HotChocolate.Data.ElasticSearch;
using HotChocolate.Data.ElasticSearch.Paging;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class ElasticSearchDataRequestBuilderExtensions
{
    /// <summary>
    /// Adds filtering support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name"></param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddElasticSearchFiltering(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        builder.ConfigureSchema(s => s.AddElasticSearchFiltering(name));

    public static IRequestExecutorBuilder AddElasticSearchSorting(this IRequestExecutorBuilder builder,
        string? name = null) => builder.ConfigureSchema(s => s.AddElasticSearchSorting(name));

    /// <summary>
    /// Adds offset and cursor pagination providers for elastic search
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="providerName"></param>
    /// <param name="defaultProvider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IRequestExecutorBuilder AddElasticSearchPagingProvider(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddOffsetPagingProvider<ElasticSearchOffsetPagingProvider>(
            providerName,
            defaultProvider);

        builder.AddCursorPagingProvider<ElasticSearchCursorPagingProvider>(
            providerName,
            defaultProvider);

        return builder;
    }
}
