using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.DataLoader
{
    public delegate Fetch<TKey, TValue> FetchFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<IReadOnlyDictionary<TKey, TValue>> Fetch<TKey, TValue>(
        IReadOnlyCollection<TKey> keys);

    public delegate FetchGrouped<TKey, TValue> FetchGroupedFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<ILookup<TKey, TValue>> FetchGrouped<TKey, TValue>(
        IReadOnlyCollection<TKey> keys);

    public delegate FetchSingle<TKey, TValue> FetchSingleFactory<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchSingle<TKey, TValue>(TKey key);

    public delegate FetchOnce<TValue> FetchOnceFactory<TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchOnce<TValue>();

    /// <summary>
    /// The DataLoader-registry holds the instances of DataLoders
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
