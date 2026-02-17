using System.Collections.Concurrent;
using GreenDonut;
using GreenDonut.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add data loader services.
/// </summary>
public static class DataLoaderServiceCollectionExtensions
{
    /// <summary>
    /// Adds a data loader for the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data loader to add.</typeparam>
    /// <param name="services">The service collection to add the data loader to.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDataLoader<T>(
        this IServiceCollection services)
        where T : class, IDataLoader
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(T)));
        services.TryAddScoped(sp => sp.GetDataLoader<T>());
        return services;
    }

    /// <summary>
    /// Adds a data loader for the specified type <typeparamref name="TService"/>
    /// and <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The service type of the data loader to add.</typeparam>
    /// <typeparam name="TImplementation">The implementation type of the data loader to add.</typeparam>
    /// <param name="services">The service collection to add the data loader to.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDataLoader<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class, IDataLoader
        where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(TService), typeof(TImplementation)));
        services.TryAddScoped(sp => sp.GetDataLoader<TImplementation>());
        services.TryAddScoped<TService>(sp => sp.GetDataLoader<TImplementation>());
        return services;
    }

    /// <summary>
    /// Adds a data loader for the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the data loader to add.</typeparam>
    /// <param name="services">The service collection to add the data loader to.</param>
    /// <param name="factory">The factory to create the data loader.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDataLoader<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T> factory)
        where T : class, IDataLoader
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(T), sp => factory(sp)));
        services.TryAddScoped(sp => sp.GetDataLoader<T>());
        return services;
    }

    /// <summary>
    /// Adds a data loader for the specified type <typeparamref name="TService"/>
    /// and <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">The service type of the data loader to add.</typeparam>
    /// <typeparam name="TImplementation">The implementation type of the data loader to add.</typeparam>
    /// <param name="services">The service collection to add the data loader to.</param>
    /// <param name="factory">The factory to create the data loader.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDataLoader<TService, TImplementation>(
        this IServiceCollection services,
        Func<IServiceProvider, TImplementation> factory)
        where TService : class, IDataLoader
        where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddDataLoaderCore();
        services.AddSingleton(new DataLoaderRegistration(typeof(TService), typeof(TImplementation), sp => factory(sp)));
        services.TryAddScoped(sp => sp.GetDataLoader<TImplementation>());
        services.TryAddScoped<TService>(sp => sp.GetDataLoader<TImplementation>());
        return services;
    }

    /// <summary>
    /// Tries to add the core data loader services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the data loader to.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection TryAddDataLoaderCore(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        services.TryAddSingleton<DataLoaderRegistrar>();
        services.AddSingleton<DataLoaderScopeFactory>();
        services.TryAddScoped(sp => sp.GetRequiredService<DataLoaderScopeFactory>().CreateScope(sp));
        services.TryAddScoped<IBatchScheduler, AutoBatchScheduler>();

        services.TryAddSingleton(sp => PromiseCachePool.Create(sp.GetRequiredService<ObjectPoolProvider>()));
        services.TryAddScoped(sp =>
        {
            var pool = sp.GetRequiredService<ObjectPool<PromiseCache>>();
            var interceptor = sp.GetService<IPromiseCacheInterceptor>();
            return new PromiseCacheOwner(pool, interceptor);
        });

        services.TryAddSingleton<IDataLoaderDiagnosticEvents>(
            sp =>
            {
                var listeners = sp.GetServices<IDataLoaderDiagnosticEventListener>().ToArray();

                return listeners.Length switch
                {
                    0 => new DataLoaderDiagnosticEventListener(),
                    1 => listeners[0],
                    _ => new AggregateDataLoaderDiagnosticEventListener(listeners)
                };
            });

        services.TryAddScoped(
            sp =>
            {
                var cacheOwner = sp.GetRequiredService<PromiseCacheOwner>();

                return new DataLoaderOptions
                {
                    Cache = cacheOwner.Cache,
                    DiagnosticEvents = sp.GetService<IDataLoaderDiagnosticEvents>(),
                    MaxBatchSize = 1024
                };
            });

        return services;
    }
}

file static class DataLoaderServiceProviderExtensions
{
    public static T GetDataLoader<T>(this IServiceProvider services) where T : IDataLoader
        => services.GetRequiredService<IDataLoaderScope>().GetDataLoader<T>();
}

internal sealed class DataLoaderScopeFactory
{
    private readonly DataLoaderRegistrar _registrar;

    public DataLoaderScopeFactory(DataLoaderRegistrar registrar)
        => _registrar = registrar;

    public IDataLoaderScope CreateScope(IServiceProvider scopedServiceProvider)
        => new DefaultDataLoaderScope(scopedServiceProvider, _registrar.Registrations);
}

file sealed class DefaultDataLoaderScope(
    IServiceProvider serviceProvider,
    IReadOnlyDictionary<Type, DataLoaderRegistration> registrations)
    : IDataLoaderScope
{
    private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders = new();

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
