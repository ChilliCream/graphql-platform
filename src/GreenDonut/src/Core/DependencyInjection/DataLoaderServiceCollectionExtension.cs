using System;
using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
using System.Linq;
using GreenDonut;
using GreenDonut.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

public static class DataLoaderServiceCollectionExtension
{
    public static IServiceCollection AddDataLoader<T>(
        this IServiceCollection services)
        where T : class, IDataLoader
    {
        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(T)));
        services.TryAddScoped<T>(sp => sp.GetDataLoader<T>());
        return services;
    }

    public static IServiceCollection AddDataLoader<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class, IDataLoader
        where TImplementation : class, TService
    {
        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(TService), typeof(TImplementation)));
        services.TryAddScoped<TImplementation>(sp => sp.GetDataLoader<TImplementation>());
        services.TryAddScoped<TService>(sp => sp.GetDataLoader<TService>());
        return services;
    }

    public static IServiceCollection AddDataLoader<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T> factory)
        where T : class, IDataLoader
    {
        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(T), sp => factory(sp)));
        services.TryAddScoped<T>(sp => sp.GetDataLoader<T>());
        return services;
    }

    public static IServiceCollection TryAddDataLoaderCore(
        this IServiceCollection services)
    {
        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        services.AddSingleton<DataLoaderContextFactory>();
        services.TryAddScoped<IDataLoaderContext>(
            sp => sp.GetRequiredService<DataLoaderContextFactory>().CreateScope(sp));
        services.TryAddScoped<IBatchScheduler>(sp => sp.GetRequiredService<IDataLoaderContext>().Scheduler);
        services.TryAddSingleton(sp => TaskCachePool.Create(sp.GetRequiredService<ObjectPoolProvider>()));
        services.TryAddScoped(sp => new TaskCacheOwner(sp.GetRequiredService<ObjectPool<TaskCache>>()));

        services.TryAddSingleton<IDataLoaderDiagnosticEvents>(
            sp =>
            {
                var listeners = sp.GetServices<IDataLoaderDiagnosticEventListener>().ToArray();

                return listeners.Length switch
                {
                    0 => new DataLoaderDiagnosticEventListener(),
                    1 => listeners[0],
                    _ => new AggregateDataLoaderDiagnosticEventListener(listeners),
                };
            });

        services.TryAddScoped(
            sp =>
            {
                var cacheOwner = sp.GetRequiredService<TaskCacheOwner>();

                return new DataLoaderOptions
                {
                    Cache = cacheOwner.Cache,
                    CancellationToken = cacheOwner.CancellationToken,
                    DiagnosticEvents = sp.GetService<IDataLoaderDiagnosticEvents>(),
                    MaxBatchSize = 1024,
                };
            });

        return services;
    }
}

file static class DataLoaderServiceProviderExtensions
{
    public static T GetDataLoader<T>(this IServiceProvider services) where T : IDataLoader
        => services.GetRequiredService<IDataLoaderContext>().GetDataLoader<T>();
}

internal sealed class DataLoaderContextFactory
{
#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<Type, DataLoaderRegistration> _registrations;
#else
    private readonly Dictionary<Type, DataLoaderRegistration> _registrations;
#endif

    public DataLoaderContextFactory(IEnumerable<DataLoaderRegistration> dataLoaderRegistrations)
#if NET8_0_OR_GREATER
        => _registrations = dataLoaderRegistrations.ToFrozenDictionary(t => t.ServiceType);
#else
        => _registrations = dataLoaderRegistrations.ToDictionary(t => t.ServiceType);
#endif

    public IDataLoaderContext CreateScope(IServiceProvider scopedServiceProvider)
        => new DefaultDataLoaderContext(
            scopedServiceProvider,
            _registrations);
}

file sealed class DefaultDataLoaderContext(
    IServiceProvider serviceProvider,
#if NET8_0_OR_GREATER
    FrozenDictionary<Type, DataLoaderRegistration> registrations)
#else
    Dictionary<Type, DataLoaderRegistration> registrations)
#endif
    : IDataLoaderContext
{
    private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders = new();

    public ActiveBatchScheduler Scheduler { get; } = new();

    public T GetDataLoader<T>(DataLoaderFactory<T> createDataLoader, string? name = null) where T : IDataLoader
    {
        name ??= CreateKey<T>();

        if (_dataLoaders.GetOrAdd(name, _ => createDataLoader(serviceProvider)) is T dataLoader)
        {
            return dataLoader;
        }

        throw new InvalidOperationException("A with the same name already exists.");
    }

    public T GetDataLoader<T>() where T : IDataLoader
        => (T)_dataLoaders.GetOrAdd(CreateKey<T>(), _ => CreateDataLoader<T>());

    private T CreateDataLoader<T>() where T : IDataLoader
    {
        if (registrations.TryGetValue(typeof(T), out var registration))
        {
            return (T)registration.CreateDataLoader(serviceProvider);
        }

        var adHocRegistration = new DataLoaderRegistration(typeof(T));
        return (T)adHocRegistration.CreateDataLoader(serviceProvider);
    }

    private static string CreateKey<T>()
        => typeof(T).FullName ?? typeof(T).Name;
}