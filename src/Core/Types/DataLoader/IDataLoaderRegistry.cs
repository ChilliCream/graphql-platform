using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Resolvers;

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

    public interface IDataLoaderRegistry
    {
        bool Register<T>(
            string key,
            Func<IServiceProvider, T> factory);

        bool TryGet<T>(
            string key,
            out T dataLoader);
    }
}
