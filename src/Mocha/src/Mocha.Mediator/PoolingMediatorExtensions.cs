using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Mocha.Mediator;

/// <summary>
/// Extension methods for registering object pooling services used by the mediator infrastructure.
/// </summary>
public static class PoolingMediatorExtensions
{
    internal static IServiceCollection AddMediatorPoolingCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ObjectPool<MediatorContext>>(_ => new MediatorContextPool());
        services.TryAddSingleton<IMediatorPools, MediatorPools>();

        return services;
    }
}
