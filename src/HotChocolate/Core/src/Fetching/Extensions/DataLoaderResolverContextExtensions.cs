using GreenDonut;
using GreenDonut.DependencyInjection;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

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
    /// <param name="name">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the value for the requested key.
    /// </returns>
    public static Task<TValue?> BatchAsync<TKey, TValue>(
        this IResolverContext context,
        FetchBatch<TKey, TValue> fetch,
        TKey key,
        string? name = null)
        where TKey : notnull
        => BatchDataLoader(context, fetch, name).LoadAsync(key, context.RequestAborted);

    /// <summary>
    /// Creates a new BatchDataLoader with the specified <paramref name="fetch"/> logic.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic.
    /// </param>
    /// <param name="name">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the DataLoader.
    /// </returns>
    public static IDataLoader<TKey, TValue> BatchDataLoader<TKey, TValue>(
        this IResolverContext context,
        FetchBatch<TKey, TValue> fetch,
        string? name = null)
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
        var scope = services.GetRequiredService<IDataLoaderScope>();
        return scope.GetDataLoader(Create, name);

        IDataLoader<TKey, TValue> Create(IServiceProvider sp)
            => new AdHocBatchDataLoader<TKey, TValue>(
                name ?? "default",
                fetch,
                sp.GetRequiredService<IBatchScheduler>(),
                sp.GetRequiredService<DataLoaderOptions>());
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
    /// <param name="name">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the value for the requested key.
    /// </returns>
    public static Task<TValue[]?> GroupAsync<TKey, TValue>(
        this IResolverContext context,
        FetchGroup<TKey, TValue> fetch,
        TKey key,
        string? name = null)
        where TKey : notnull
        => GroupDataLoader(context, fetch, name).LoadAsync(key, context.RequestAborted);

    /// <summary>
    /// Creates a new GroupDataLoader with the specified <paramref name="fetch"/> logic.
    /// </summary>
    /// <param name="context">
    /// The resolver context.
    /// </param>
    /// <param name="fetch">
    /// The batch fetch logic for the GroupDataLoader.
    /// </param>
    /// <param name="name">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the DataLoader.
    /// </returns>
    public static IDataLoader<TKey, TValue[]> GroupDataLoader<TKey, TValue>(
        this IResolverContext context,
        FetchGroup<TKey, TValue> fetch,
        string? name = null)
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
        var scope = services.GetRequiredService<IDataLoaderScope>();
        return scope.GetDataLoader(Create, name);

        IDataLoader<TKey, TValue[]> Create(IServiceProvider sp)
            => new AdHocGroupedDataLoader<TKey, TValue>(
                name ?? "default",
                fetch,
                sp.GetRequiredService<IBatchScheduler>(),
                sp.GetRequiredService<DataLoaderOptions>());
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
    /// <param name="name">
    /// The optional DataLoader name.
    /// </param>
    /// <returns>
    /// Returns the value for the requested key.
    /// </returns>
    public static Task<TValue?> CacheAsync<TKey, TValue>(
        this IResolverContext context,
        FetchCache<TKey, TValue> fetch,
        TKey key,
        string? name = null)
        where TKey : notnull
        => CacheDataLoader(context, fetch, name).LoadAsync(key, context.RequestAborted);

    public static IDataLoader<TKey, TValue> CacheDataLoader<TKey, TValue>(
        this IResolverContext context,
        FetchCache<TKey, TValue> fetch,
        string? name = null)
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
        var scope = services.GetRequiredService<IDataLoaderScope>();
        return scope.GetDataLoader(Create, name);

        IDataLoader<TKey, TValue> Create(IServiceProvider sp)
            => new AdHocCacheDataLoader<TKey, TValue>(
                name ?? "default",
                fetch,
                services.GetRequiredService<DataLoaderOptions>());
    }

    public static Task<TValue?> CacheAsync<TValue>(
        this IResolverContext context,
        Func<CancellationToken, Task<TValue>> fetch,
        string? name = null)
        => FetchOnceAsync(context, fetch, name);

    public static Task<TValue?> FetchOnceAsync<TValue>(
        this IResolverContext context,
        Func<CancellationToken, Task<TValue>> fetch,
        string? name = null)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (fetch is null)
        {
            throw new ArgumentNullException(nameof(fetch));
        }

        return CacheDataLoader<string, TValue>(context, (_, ct) => fetch(ct), name)
            .LoadAsync("default", context.RequestAborted);
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
        var reg = services.GetRequiredService<IDataLoaderScope>();
        return reg.GetDataLoader<T>();
    }
}
