using System;
using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.Generic;
#endif
using GreenDonut;
using HotChocolate.Fetching.Properties;

namespace HotChocolate.Fetching;

internal sealed class DefaultDataLoaderScope(
    IServiceProvider serviceProvider,
#if NET8_0_OR_GREATER
    FrozenDictionary<Type, DataLoaderRegistration> registrations)
#else
    Dictionary<Type, DataLoaderRegistration> registrations)
#endif
    : IDataLoaderScope
{
    private readonly ConcurrentDictionary<string, IDataLoader> _dataLoaders = new();

    public T GetDataLoader<T>(Func<T> createDataLoader, string? name = null) where T : IDataLoader
    {
        name ??= CreateKey<T>();

        if (_dataLoaders.GetOrAdd(name, _ => createDataLoader()) is T dataLoader)
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
        if (!registrations.TryGetValue(typeof(T), out var registration))
        {
            throw new RegisterDataLoaderException("NO DATALOADER!");
        }

        return (T)registration.CreateDataLoader(serviceProvider);
    }

    private static string CreateKey<T>()
        => typeof(T).FullName ?? typeof(T).Name;
}