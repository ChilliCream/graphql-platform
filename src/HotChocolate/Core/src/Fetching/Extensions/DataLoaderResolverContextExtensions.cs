using System;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fetching.Properties.FetchingResources;

#nullable enable

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class DataLoaderResolverContextExtensions
{
    /// <summary>
    /// This utility methods creates a new <see cref="GreenDonut.BatchDataLoader{TKey,TValue}" />
    /// with the provided <paramref name="fetch"/> logic and invoked the
    /// <see cref="IDataLoader{TKey,TValue}.LoadAsync(TKey,CancellationToken)"/> with
    /// the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic.
    /// </param>
    /// <param name="key">
    /// The key to fetch.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the value for the requested key.
    /// </returns>
    public static Task<TValue> BatchAsync<TKey, TValue>(
        this IResolverContext context,
        FetchBatch<TKey, TValue> fetch,
        TKey key,
        string? dataLoaderName = null)
        where TKey : notnull
        => BatchDataLoader(context, fetch, dataLoaderName).LoadAsync(key, context.RequestAborted);

    /// <summary>
    /// Creates a new BatchDataLoader with the specified <paramref name="fetch"/> logic.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the DataLoader.
    /// </returns>
    public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
        this IResolverContext context,
        FetchBatch<TKey, TValue> fetch,
        string? dataLoaderName = null)
        where TKey : notnull
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (fetch is null)
        {
            throw new ArgumentNullException(nameof(fetch));
        }

        var services = context.RequestServices;
        var reg = services.GetRequiredService<IDataLoaderRegistry>();
        FetchBatchDataLoader<TKey, TValue> Loader()
            => new(
                dataLoaderName ?? "default",
                fetch,
                services.GetRequiredService<IBatchScheduler>(),
                services.GetRequiredService<DataLoaderOptions>());

        return dataLoaderName is null
            ? reg.GetOrRegister(Loader)
            : reg.GetOrRegister(dataLoaderName, Loader);
    }

    /// <summary>
    /// Creates a new batch DataLoader with the specified <paramref name="fetch"/> logic.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic.
    /// </param>
    /// <returns>
    /// Returns the DataLoader.
    /// </returns>
    [Obsolete]
    public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
        this IResolverContext context,
        string dataLoaderName,
        FetchBatch<TKey, TValue> fetch)
        where TKey : notnull
    {
        if (string.IsNullOrEmpty(dataLoaderName))
        {
            throw new ArgumentException(
                DataLoaderRegistry_KeyNullOrEmpty,
                nameof(dataLoaderName));
        }

        return BatchDataLoader(context, fetch, dataLoaderName);
    }

    /// <summary>
    /// This utility methods creates a new <see cref="GroupedDataLoader{TKey,TValue}" />
    /// with the provided <paramref name="fetch"/> logic and invoked the
    /// <see cref="IDataLoader{TKey,TValue}.LoadAsync(TKey,CancellationToken)"/> with
    /// the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic for a GroupDataLoader.
    /// </param>
    /// <param name="key">
    /// The key to fetch.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the value for the requested key.
    /// </returns>
    public static Task<TValue[]> GroupAsync<TKey, TValue>(
        this IResolverContext context,
        FetchGroup<TKey, TValue> fetch,
        TKey key,
        string? dataLoaderName = null)
        where TKey : notnull
        => GroupDataLoader(context, fetch, dataLoaderName).LoadAsync(key, context.RequestAborted);

    /// <summary>
    /// Creates a new GroupDataLoader with the specified <paramref name="fetch"/> logic.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic for the GroupDataLoader.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the DataLoader.
    /// </returns>
    public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
        this IResolverContext context,
        FetchGroup<TKey, TValue> fetch,
        string? dataLoaderName = null)
        where TKey : notnull
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (fetch is null)
        {
            throw new ArgumentNullException(nameof(fetch));
        }

        var services = context.RequestServices;
        var reg = services.GetRequiredService<IDataLoaderRegistry>();
        FetchGroupedDataLoader<TKey, TValue> Loader()
            => new(
                dataLoaderName ?? "default",
                fetch,
                services.GetRequiredService<IBatchScheduler>(),
                services.GetRequiredService<DataLoaderOptions>());

        return dataLoaderName is null
            ? reg.GetOrRegister(Loader)
            : reg.GetOrRegister(dataLoaderName, Loader);
    }

    /// <summary>
    /// Creates a new GroupDataLoader with the specified <paramref name="fetch"/> logic.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic for the GroupDataLoader.
    /// </param>
    /// <returns>
    /// Returns the DataLoader.
    /// </returns>
    [Obsolete]
    public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
        this IResolverContext context,
        string dataLoaderName,
        FetchGroup<TKey, TValue> fetch)
        where TKey : notnull
    {
        if (string.IsNullOrEmpty(dataLoaderName))
        {
            throw new ArgumentException(
                DataLoaderRegistry_KeyNullOrEmpty,
                nameof(dataLoaderName));
        }

        return GroupDataLoader(context, fetch, dataLoaderName);
    }

    /// <summary>
    /// This utility methods creates a new <see cref="GreenDonut.CacheDataLoader{TKey,TValue}" />
    /// with the provided <paramref name="fetch"/> logic and invoked the
    /// <see cref="IDataLoader{TKey,TValue}.LoadAsync(TKey,CancellationToken)"/> with
    /// the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The fetch logic for a CacheDataLoader.
    /// </param>
    /// <param name="key">
    /// The key to fetch.
    /// </param>
    /// <param name="dataLoaderName">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the value for the requested key.
    /// </returns>
    public static Task<TValue> CacheAsync<TKey, TValue>(
        this IResolverContext context,
        FetchCache<TKey, TValue> fetch,
        TKey key,
        string? dataLoaderName = null)
        where TKey : notnull
        => CacheDataLoader(context, fetch, dataLoaderName).LoadAsync(key, context.RequestAborted);

    public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
        this IResolverContext context,
        FetchCache<TKey, TValue> fetch,
        string? key = null)
        where TKey : notnull
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (fetch is null)
        {
            throw new ArgumentNullException(nameof(fetch));
        }

        var services = context.RequestServices;
        var reg = services.GetRequiredService<IDataLoaderRegistry>();
        FetchCacheDataLoader<TKey, TValue> Loader()
            => new(
                key ?? "default",
                fetch,
                services.GetRequiredService<DataLoaderOptions>());

        return key is null
            ? reg.GetOrRegister(Loader)
            : reg.GetOrRegister(key, Loader);
    }

    [Obsolete]
    public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
        this IResolverContext context,
        string key,
        FetchCache<TKey, TValue> fetch)
        where TKey : notnull
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                DataLoaderRegistry_KeyNullOrEmpty,
                nameof(key));
        }

        return CacheDataLoader(context, fetch, key);
    }

    public static Task<TValue> CacheAsync<TValue>(
        this IResolverContext context,
        Func<CancellationToken, Task<TValue>> fetch,
        string? dataLoaderName = null)
        => FetchOnceAsync(context, fetch, dataLoaderName);

    public static Task<TValue> FetchOnceAsync<TValue>(
        this IResolverContext context,
        Func<CancellationToken, Task<TValue>> fetch,
        string? key = null)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (fetch is null)
        {
            throw new ArgumentNullException(nameof(fetch));
        }

        return CacheDataLoader<string, TValue>(
            context,
            (_, ct) => fetch(ct),
            key)
            .LoadAsync("default", context.RequestAborted);
    }

    [Obsolete]
    public static Task<TValue> FetchOnceAsync<TValue>(
        this IResolverContext context,
        string key,
        Func<CancellationToken, Task<TValue>> fetch)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                DataLoaderRegistry_KeyNullOrEmpty,
                nameof(key));
        }

        return FetchOnceAsync(context, fetch, key);
    }

    [GetDataLoader]
    public static T DataLoader<T>(this IResolverContext context)
        where T : IDataLoader
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var services = context.RequestServices;
        var reg = services.GetRequiredService<IDataLoaderRegistry>();
        return reg.GetOrRegister(() => CreateDataLoader<T>(services));
    }

    private static T CreateDataLoader<T>(IServiceProvider services)
        where T : IDataLoader
    {
        var registeredDataLoader = services.GetService<T>();

        if (registeredDataLoader is null)
        {
            if (typeof(T).IsInterface || typeof(T).IsAbstract)
            {
                throw new RegisterDataLoaderException(
                    string.Format(
                        DataLoaderResolverContextExtensions_CreateDataLoader_AbstractType,
                        typeof(T).FullName ?? typeof(T).Name));
            }

            var factory = new ServiceFactory { Services = services, };
            if (factory.CreateInstance(typeof(T)) is T dataLoader)
            {
                return dataLoader;
            }

            throw new RegisterDataLoaderException(
                string.Format(
                    DataLoaderResolverContextExtensions_CreateDataLoader_UnableToCreate,
                    typeof(T).FullName ?? typeof(T).Name));
        }

        return registeredDataLoader;
    }
}

internal sealed class GetDataLoaderAttribute : Attribute;
