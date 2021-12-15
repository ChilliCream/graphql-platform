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
/// various service behaviours like pooled services.
/// </summary>
internal static class ServiceHelper
{
    private const BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly MethodInfo _usePooledService =
        typeof(ServiceHelper).GetMethod(nameof(UsePooledServiceInternal), _flags)!;
    private static readonly MethodInfo _useResolverService =
        typeof(ServiceHelper).GetMethod(nameof(UseResolverServiceInternal), _flags)!;

    internal static void UsePooledService(
        ObjectFieldDefinition definition,
        Type serviceType)
        => _usePooledService
            .MakeGenericMethod(serviceType)
            .Invoke(null, new object?[] {definition});

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
                    IServiceProvider services = context.Services;
                    var objectPool = services.GetRequiredService<ObjectPool<TService>>();
                    TService service = objectPool.Get();

                    try
                    {
                        context.SetLocalValue(scopedServiceName, service);
                        await next(context).ConfigureAwait(false);
                    }
                    finally
                    {
                        objectPool.Return(service!);
                    }
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
            .Invoke(null, new object?[] {definition});

    private static void UseResolverServiceInternal<TService>(
        ObjectFieldDefinition definition)
        where TService : class
    {
        var scopedServiceName = typeof(TService).FullName ?? typeof(TService).Name;

        FieldMiddlewareDefinition? middleware =
            definition.MiddlewareDefinitions.FirstOrDefault(
                t => t.Key == WellKnownMiddleware.ResolverServiceScope);
        var index = 0;

        if (middleware is null)
        {
            middleware = new FieldMiddlewareDefinition(
                next => async context =>
                {
                    using IServiceScope scope = context.Services.CreateScope();
                    context.SetLocalValue(WellKnownContextData.ResolverServiceScope, scope);
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
                    IServiceScope? scope = context.GetLocalValue<IServiceScope>(
                        WellKnownContextData.ResolverServiceScope);

                    if (scope is null)
                    {
                        throw new InvalidOperationException(
                            TypeResources.ServiceHelper_UseResolverServiceInternal_Order);
                    }

                    TService service = scope.ServiceProvider.GetRequiredService<TService>();
                    context.SetLocalValue(scopedServiceName, service);
                    await next(context).ConfigureAwait(false);
                },
                isRepeatable: true,
                key: WellKnownMiddleware.PooledService);
        definition.MiddlewareDefinitions.Insert(index + 1, serviceMiddleware);
    }
}
