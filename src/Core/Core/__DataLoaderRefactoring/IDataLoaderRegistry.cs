using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Runtime;

namespace HotChocolate
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

    public delegate FetchOnce<TKey, TValue> FetchOnce<TKey, TValue>(
        IServiceProvider services);

    public delegate Task<TValue> FetchOnce<TValue>();

    public interface IDataLoaderRegistry
        : IBatchedOperation
    {
        bool Register<TKey, TValue>(
            string key,
            FetchFactory<TKey, TValue> fetch);

        bool Register<TKey, TValue>(
            string key,
            FetchGroupedFactory<TKey, TValue> fetch);

        bool Register<TKey, TValue>(
            string key,
            FetchSingleFactory<TKey, TValue> fetch);

        bool Register<T>(
            string key,
            Func<IServiceProvider, T> factory);

        bool TryGet<T>(string key, out T dataLoader);
    }

    public interface IBatchedOperation
    {
        event EventHandler<EventArgs> BatchSizeIncreased;

        /// <summary>
        /// Gets count of items in the current batch.
        /// </summary>
        int BatchSize { get; }

        /// <summary>
        /// Executes the current batch
        /// </summary>
        /// <returns></returns>
        Task InvokeAsync();
    }

    internal class BatchedOperationHandler
   {

    }
}
