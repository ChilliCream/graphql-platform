using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Runtime;

namespace HotChocolate
{
    public delegate Task<IReadOnlyDictionary<TKey, TValue>> Fetch<TKey, TValue>(IReadOnlyCollection<TKey> keys);

    public delegate Task<ILookup<TKey, TValue>> FetchGrouped<TKey, TValue>(IReadOnlyCollection<TKey> keys);

    public delegate Task<TValue> FetchSingle<TKey, TValue>(TKey key);

    public delegate Task<TValue> FetchOnce<TValue>();

    public interface IDataLoaderRegistry
    {

        IDataLoader<TKey, TValue> GetOrCreate<TKey, TValue>(string key, ExecutionScope scope, Fetch<TKey, TValue> fetch);

        IDataLoader<TKey, IEnumerable<TValue>> GetOrCreate<TKey, TValue>(string key, ExecutionScope scope, FetchGrouped<TKey, TValue> fetch);

        IDataLoader<TKey, TValue> GetOrCreate<TKey, TValue>(string key, ExecutionScope scope, FetchSingle<TKey, TValue> fetch);

        // TODO extensions method
        IDataLoader<TKey, TValue> GetOrCreate<TKey, TValue>(string key, ExecutionScope scope, FetchOnce<TValue> fetch);

        T GetOrCreate<T>(string key, ExecutionScope scope);

        IDisposable CreateScope(params string[] keys);

        // IDataLoader<TKey, TValue> GetOrCreate<TKey, TValue>(FetchSingle<TValue> fetch);
    }

    public interface IBatchedOperation
    {
        Task InvokeAsync();
    }

    public class Test
    {
        public void TestMe(Task t)
        {
            t.Status
        }
    }
}
