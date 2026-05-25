using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExecutionServiceProviderExtensions
{
    /// <summary>
    /// Gets the root service provider from the schema services. This allows
    /// schema services to access application level services.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <returns>
    /// The root service provider.
    /// </returns>
    public static IServiceProvider GetRootServiceProvider(this ISchemaDefinition schema)
        => schema.Services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;

    /// <summary>
    /// Gets the root service provider from the schema services. This allows
    /// schema services to access application level services.
    /// </summary>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The root service provider.
    /// </returns>
    public static IServiceProvider GetRootServiceProvider(this IServiceProvider services)
        => services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;
}
