using HotChocolate.Data.Neo4J.Paging;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Neo4J;

/// <summary>
/// Provides data extensions for the <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class Neo4JDataRequestBuilderExtensions
{
    /// <summary>
    /// Adds filtering support for Neo4j.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The filtering convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddNeo4JFiltering(
        this IRequestExecutorBuilder builder,
        string? name = null,
        bool compatabilityMode = false) =>
        builder.ConfigureSchema(s => s.AddNeo4JFiltering(name, compatabilityMode));

    /// <summary>
    /// Adds sorting support for Neo4j.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The sorting convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddNeo4JSorting(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        builder.ConfigureSchema(s => s.AddNeo4JSorting(name));

    /// <summary>
    /// Adds projections support for Neo4j.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The projection convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddNeo4JProjections(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        builder.ConfigureSchema(s => s.AddNeo4JProjections(name));

    /// <summary>
    /// Adds the Neo4j offset paging provider.
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
    public static IRequestExecutorBuilder AddNeo4JPagingProviders(
        this IRequestExecutorBuilder builder,
        string? providerName = null,
        bool defaultProvider = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddOffsetPagingProvider<Neo4JOffsetPagingProvider>(
            providerName,
            defaultProvider);

        return builder;
    }
}
