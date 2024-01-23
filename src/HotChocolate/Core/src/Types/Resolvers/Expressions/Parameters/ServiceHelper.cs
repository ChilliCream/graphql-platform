using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// The service helper configures the object fields with middleware to handle
/// various service behaviors like pooled services.
/// </summary>
internal static class ServiceHelper
{
    private const BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly MethodInfo _usePooledService =
        typeof(ServiceHelper).GetMethod(nameof(UsePooledServiceInternal), _flags)!;
    private static readonly MethodInfo _useResolverService =
        typeof(ServiceHelper).GetMethod(nameof(UseResolverServiceInternal), _flags)!;
#if NET8_0_OR_GREATER
    private static readonly MethodInfo _useResolverKeyedService =
        typeof(ServiceHelper).GetMethod(nameof(UseResolverKeyedServiceInternal), _flags)!;
#endif

    internal static void UsePooledService(
        ObjectFieldDefinition definition,
        Type serviceType)
        => _usePooledService
            .MakeGenericMethod(serviceType)
            .Invoke(null, [definition,]);

    internal static void UsePooledService<TService>(
        ObjectFieldDefinition definition)
        where TService : class
        => UsePooledServiceInternal<TService>(definition);

    private static void UsePooledServiceInternal<TService>(
        ObjectFieldDefinition definition)
        where TService : class
    {
        var scopedServiceName = typeof(TService).FullName ?? typeof(TService).Name;

        FieldMiddlewareDefinition serviceMiddleware =
            new(next => async context =>
                {
                    var services = context.RequestServices;
                    var objectPool = services.GetRequiredService<ObjectPool<TService>>();
                    var service = objectPool.Get();

                    context.RegisterForCleanup(() =>
                    {
                        objectPool.Return(service);
                        return default;
                    });

                    context.SetLocalState(scopedServiceName, service);
                    await next(context).ConfigureAwait(false);
                },
                isRepeatable: true,
                key: WellKnownMiddleware.PooledService);

        definition.MiddlewareDefinitions.Insert(0, serviceMiddleware);
    }

    internal static void UseResolverService(
        ObjectFieldDefinition definition,
        Type serviceType)
        => _useResolverService
            .MakeGenericMethod(serviceType)
            .Invoke(null, [definition,]);

    private static void UseResolverServiceInternal<TService>(
        ObjectFieldDefinition definition)
        where TService : class
    {
        var scopedServiceName = typeof(TService).FullName ?? typeof(TService).Name;

        var middleware =
            definition.MiddlewareDefinitions.FirstOrDefault(
                t => t.Key == WellKnownMiddleware.ResolverServiceScope);
        var index = 0;

        if (middleware is null)
        {
            middleware = new FieldMiddlewareDefinition(
                next => async context =>
                {
                    var service = context.RequestServices;
                    var scope = service.CreateScope();
                    context.RegisterForCleanup(() =>
                    {
                        scope.Dispose();
                        return default;
                    });
                    context.SetLocalState(WellKnownContextData.ResolverServiceScope, scope);
                    await next(context).ConfigureAwait(false);
                },
                isRepeatable: false,
                key: WellKnownMiddleware.ResolverServiceScope);
            definition.MiddlewareDefinitions.Insert(index, middleware);
        }
        else
        {
            index = definition.MiddlewareDefinitions.IndexOf(middleware);
        }

        FieldMiddlewareDefinition serviceMiddleware =
            new(next => async context =>
                {
                    var scope = context.GetLocalStateOrDefault<IServiceScope>(
                        WellKnownContextData.ResolverServiceScope);

                    if (scope is null)
                    {
                        throw new InvalidOperationException(
                            TypeResources.ServiceHelper_UseResolverServiceInternal_Order);
                    }

                    var service = scope.ServiceProvider.GetRequiredService<TService>();
                    context.SetLocalState(scopedServiceName, service);
                    await next(context).ConfigureAwait(false);
                },
                isRepeatable: true,
                key: WellKnownMiddleware.ResolverService);
        definition.MiddlewareDefinitions.Insert(index + 1, serviceMiddleware);
    }

#if NET8_0_OR_GREATER
    internal static void UseResolverKeyedService(
        ObjectFieldDefinition definition,
        Type serviceType,
        string key)
        => _useResolverKeyedService
            .MakeGenericMethod(serviceType)
            .Invoke(null, [definition, key,]);

    private static void UseResolverKeyedServiceInternal<TService>(
        ObjectFieldDefinition definition,
        string key)
        where TService : class
    {
        var scopedServiceName = $"{key}:{typeof(TService).FullName ?? typeof(TService).Name}";

        var middleware =
            definition.MiddlewareDefinitions.FirstOrDefault(
                t => t.Key == WellKnownMiddleware.ResolverServiceScope);
        var index = 0;

        if (middleware is null)
        {
            middleware = new FieldMiddlewareDefinition(
                next => async context =>
                {
                    var service = context.RequestServices;
                    var scope = service.CreateScope();
                    context.RegisterForCleanup(() =>
                    {
                        scope.Dispose();
                        return default;
                    });
                    context.SetLocalState(WellKnownContextData.ResolverServiceScope, scope);
                    await next(context).ConfigureAwait(false);
                },
                isRepeatable: false,
                key: WellKnownMiddleware.ResolverServiceScope);
            definition.MiddlewareDefinitions.Insert(index, middleware);
        }
        else
        {
            index = definition.MiddlewareDefinitions.IndexOf(middleware);
        }

        FieldMiddlewareDefinition serviceMiddleware =
            new(next => async context =>
                {
                    var scope = context.GetLocalStateOrDefault<IServiceScope>(
                        WellKnownContextData.ResolverServiceScope);

                    if (scope is null)
                    {
                        throw new InvalidOperationException(
                            TypeResources.ServiceHelper_UseResolverServiceInternal_Order);
                    }

                    var service = scope.ServiceProvider.GetRequiredKeyedService<TService>(key);
                    context.SetLocalState(scopedServiceName, service);
                    await next(context).ConfigureAwait(false);
                },
                isRepeatable: true,
                key: WellKnownMiddleware.ResolverService);
        definition.MiddlewareDefinitions.Insert(index + 1, serviceMiddleware);
    }
#endif
}
