using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Sagas;

/// <summary>
/// Extension methods for registering the in-memory saga store.
/// </summary>
public static class InMemorySagaServiceCollectionExtensions
{
    /// <summary>
    /// Adds an in-memory saga store to the service collection.
    /// </summary>
    /// <remarks>
    /// The in-memory store is suitable for development, testing, and scenarios
    /// where saga state persistence across process restarts is not required.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemorySagas(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemorySagaStateStorage>();
        services.TryAddScoped<ISagaStore, InMemorySagaStore>();

        return services;
    }
}
