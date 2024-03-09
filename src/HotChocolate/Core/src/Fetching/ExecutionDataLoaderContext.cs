using System;
using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.Generic;
#endif
using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Fetching.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fetching;

internal sealed class ExecutionDataLoaderContext : IDataLoaderContext
{
    private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders = new();
    private readonly IServiceProvider _serviceProvider;
#if NET8_0_OR_GREATER
    private readonly FrozenDictionary<Type, DataLoaderRegistration> _registrations;
#else
    private readonly Dictionary<Type, DataLoaderRegistration> _registrations;
#endif
    
    public ExecutionDataLoaderContext(IServiceProvider serviceProvider,
#if NET8_0_OR_GREATER
        FrozenDictionary<Type, DataLoaderRegistration> registrations)
#else
        Dictionary<Type, DataLoaderRegistration> registrations)
#endif
    {
        _registrations = registrations;
        _serviceProvider = new DataLoaderServiceProvider(serviceProvider, Scheduler);
    }

    public ActiveBatchScheduler Scheduler { get; } = new();

    public T GetDataLoader<T>(DataLoaderFactory<T> createDataLoader, string? name = null) where T : IDataLoader
    {
        name ??= CreateKey<T>();

        if (_dataLoaders.GetOrAdd(name, _ => createDataLoader(_serviceProvider)) is T dataLoader)
        {
            return dataLoader;
        }

        throw new RegisterDataLoaderException(
            string.Format(
                FetchingResources.DefaultDataLoaderRegistry_GetOrRegister,
                name,
                typeof(T).FullName));
    }

    public T GetDataLoader<T>() where T : IDataLoader
        => (T)_dataLoaders.GetOrAdd(CreateKey<T>(), _ => CreateDataLoader<T>());

    private T CreateDataLoader<T>() where T : IDataLoader
    {
        if (_registrations.TryGetValue(typeof(T), out var registration))
        {
            return (T)registration.CreateDataLoader(_serviceProvider);
        }

        var adHocRegistration = new DataLoaderRegistration(typeof(T));
        return (T)adHocRegistration.CreateDataLoader(_serviceProvider);
    }

    private static string CreateKey<T>()
        => typeof(T).FullName ?? typeof(T).Name;

    private class DataLoaderServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _innerServiceProvider;
        private readonly IServiceProviderIsService? _serviceInspector;
        private readonly IBatchScheduler _batchScheduler;

        public DataLoaderServiceProvider(IServiceProvider innerServiceProvider, IBatchScheduler batchScheduler)
        {   
            _innerServiceProvider = innerServiceProvider;
            _batchScheduler = batchScheduler;
            var serviceInspector = innerServiceProvider.GetService<IServiceProviderIsService>();
            _serviceInspector = serviceInspector is not null
                ? new CombinedServiceProviderIsService(serviceInspector)
                : null;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (serviceType == typeof(IServiceProviderIsService))
            {
                return _serviceInspector;
            }
            
            if(serviceType == typeof(IBatchScheduler))
            {
                return _batchScheduler;
            }

            return _innerServiceProvider.GetService(serviceType);
        }

        private sealed class CombinedServiceProviderIsService(
            IServiceProviderIsService innerIsServiceInspector)
            : IServiceProviderIsService
        {
            public bool IsService(Type serviceType)
                => typeof(IBatchScheduler) == serviceType || 
                    innerIsServiceInspector.IsService(serviceType);
        }
    }
}