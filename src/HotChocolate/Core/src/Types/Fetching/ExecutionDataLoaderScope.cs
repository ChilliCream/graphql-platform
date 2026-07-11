using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Properties;

namespace HotChocolate.Fetching;

internal sealed class ExecutionDataLoaderScope(
    IServiceProvider serviceProvider,
    IBatchScheduler batchScheduler,
    IReadOnlyDictionary<Type, DataLoaderRegistration> registrations)
    : IDataLoaderScope
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private Dictionary<string, IDataLoader>? _dataLoaders;

    private readonly IServiceProvider _serviceProvider = new DataLoaderServiceProvider(serviceProvider, batchScheduler);

    public T GetDataLoader<T>(DataLoaderFactory<T> createDataLoader, string? name = null) where T : IDataLoader
    {
        name ??= CreateKey<T>();

        IDataLoader dataLoader;

        lock (_sync)
        {
            _dataLoaders ??= [];

            if (!_dataLoaders.TryGetValue(name, out dataLoader!))
            {
                dataLoader = createDataLoader(_serviceProvider);
                _dataLoaders.Add(name, dataLoader);
            }
        }

        if (dataLoader is T typed)
        {
            return typed;
        }

        throw new RegisterDataLoaderException(
            string.Format(
                FetchingResources.DefaultDataLoaderRegistry_GetOrRegister,
                name,
                typeof(T).FullName));
    }

    public T GetDataLoader<T>() where T : IDataLoader
    {
        var key = CreateKey<T>();

        lock (_sync)
        {
            _dataLoaders ??= [];

            if (_dataLoaders.TryGetValue(key, out var existing))
            {
                return (T)existing;
            }

            var created = CreateDataLoader<T>();
            _dataLoaders.Add(key, created);
            return created;
        }
    }

    private T CreateDataLoader<T>() where T : IDataLoader
    {
        if (registrations.TryGetValue(typeof(T), out var registration))
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
            ArgumentNullException.ThrowIfNull(serviceType);

            if (serviceType == typeof(IServiceProviderIsService))
            {
                return _serviceInspector;
            }

            if (serviceType == typeof(IBatchScheduler))
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
                => typeof(IBatchDispatcher) == serviceType
                    || innerIsServiceInspector.IsService(serviceType);
        }
    }
}
