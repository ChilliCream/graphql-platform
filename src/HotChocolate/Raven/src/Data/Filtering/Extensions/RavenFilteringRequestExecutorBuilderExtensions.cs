using HotChocolate.Data;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Common extensions of <see cref="IRequestExecutorBuilder"/> for RavenDB
/// </summary>
public static class RavenFilteringRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds filtering for RavenDB to the schema
    /// </summary>
    /// <param name="builder">The schema builder</param>
    /// <returns>The schema builder of parameter <paramref name="builder"/></returns>
    public static IRequestExecutorBuilder AddRavenFiltering(this IRequestExecutorBuilder builder)
        => builder.RegisterDocumentStore().ConfigureSchema(x => x.AddRavenFiltering());
}
