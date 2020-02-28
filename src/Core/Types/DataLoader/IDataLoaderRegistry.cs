using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    public delegate FetchBatch<TKey, TValue> FetchBatchFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<IReadOnlyDictionary<TKey, TValue>> FetchBatch<TKey, TValue>(
        IReadOnlyList<TKey> keys);

    public delegate Task<IReadOnlyDictionary<TKey, TValue>> FetchBatchCt<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken);

    public delegate FetchGroup<TKey, TValue> FetchGroupeFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<ILookup<TKey, TValue>> FetchGroup<TKey, TValue>(
        IReadOnlyList<TKey> keys);

    public delegate Task<ILookup<TKey, TValue>> FetchGroupCt<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken);

    public delegate FetchCache<TKey, TValue> FetchCacheFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchCache<TKey, TValue>(TKey key);

    public delegate Task<TValue> FetchCacheCt<TKey, TValue>(
        TKey key,
        CancellationToken cancellationToken);

    public delegate FetchOnce<TValue> FetchOnceFactory<TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchOnce<TValue>();

    public delegate Task<TValue> FetchOnceCt<TValue>(
        CancellationToken cancellationToken);

    /// <summary>
    /// The DataLoader-registry holds the instances of DataLoaders
    /// that are used by the execution engine.
    /// </summary>
    public interface IDataLoaderRegistry
        : IObservable<IDataLoader>
    {
        /// <summary>
        /// Registers a new DataLoader with this registry.
        /// </summary>
        /// <param name="key">
        /// The key with which this DataLoader can be resolved.
        /// </param>
        /// <param name="factory">
        /// The factory that can create a instance of the DataLoader
        /// when it is needed.
        /// </param>
        /// <typeparam name="T">
        /// The DataLoader type.
        /// </typeparam>
        /// <returns>
        /// Returns <c>true</c> if a DataLoader was successfully
        /// registered for the specified <paramref name="key"/>;
        /// otherwise, <c>false</c> will be returned.
        /// </returns>
        bool Register<T>(string key, Func<IServiceProvider, T> factory)
            where T : IDataLoader;

        /// <summary>
        /// Tries to retrieve a DataLoader with the specified
        /// <paramref name="key" />.
        /// </summary>
        /// <param name="key">
        /// The key with which this DataLoader can be resolved.
        /// </param>
        /// <param name="dataLoader">
        /// The retrieved DataLoader instance or <c>null</c>
        /// if there is no DataLoader registered for the specified
        /// <paramref name="key" />.
        /// </param>
        /// <typeparam name="T">
        /// The DataLoader type.
        /// </typeparam>
        /// <returns>
        /// Returns <c>true</c> if a DataLoader was resolved;
        /// otherwise, <c>false</c> will be returned.
        /// </returns>
        bool TryGet<T>(string key, out T dataLoader)
            where T : IDataLoader;
    }
}
