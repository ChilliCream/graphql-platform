using HotChocolate.Execution.Configuration;
using HotChocolate.Data.ElasticSearch;

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
}
