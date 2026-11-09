using HotChocolate.Data;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IRequestExecutorBuilder"/> for Elasticsearch.
/// </summary>
public static class ElasticSearchSortingRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds sorting over ES|QL to the schema.
    /// </summary>
    /// <param name="builder">The request executor builder.</param>
    /// <returns>The request executor builder.</returns>
    public static IRequestExecutorBuilder AddElasticSearchSorting(this IRequestExecutorBuilder builder)
        => builder.ConfigureSchema(x => x.AddElasticSearchSorting());
}
