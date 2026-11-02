using HotChocolate.Caching.Memory;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Projections;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for projection selector caching.
/// </summary>
public static class HotChocolateExecutionProjectionsRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds the projection selector cache to the schema services.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="capacity">
    /// The maximum number of selector variants retained by the cache.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddProjectionSelectorCache(
        this IRequestExecutorBuilder builder,
        int capacity = 4096)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        return builder.ConfigureSchemaServices(
            services =>
            {
                services.RemoveAll<ProjectionSelectorCache>();
                services.AddSingleton(
                    sp => new ProjectionSelectorCache(
                        capacity,
                        sp.GetService<CacheDiagnostics>()));
            });
    }
}
