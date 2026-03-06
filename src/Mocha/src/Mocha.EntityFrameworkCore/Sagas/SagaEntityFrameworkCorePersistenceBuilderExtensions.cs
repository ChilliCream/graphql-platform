using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Sagas;
using Mocha.Sagas.EfCore;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the core saga store backed by Entity Framework Core.
/// </summary>
public static class SagaEntityFrameworkCorePersistenceBuilderExtensions
{
    /// <summary>
    /// Registers a scoped <see cref="ISagaStore"/> implementation that uses the configured DbContext
    /// for saga state persistence via EF Core change tracking.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder AddSagaCore(this IEntityFrameworkCoreBuilder builder)
    {
        var contextType = builder.ContextType;

        builder.Services.TryAddScoped<ISagaStore>(sp => DbContextSagaStore.Create(contextType, sp));

        return builder;
    }
}
