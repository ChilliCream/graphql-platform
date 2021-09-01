using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenDonut
{
    public interface IDataLoaderDiagnosticEvents
    {
        void ResolvedTaskFromCache(
            TaskCacheKey cacheKey,
            Task task);

        IActivityScope ExecuteBatch<TKey>(
            IDataLoader dataLoader,
            IReadOnlyList<TKey> keys);

        void BatchResults<TKey, TValue>(
            IActivityScope scope,
            IReadOnlyList<TKey> keys,
            ReadOnlySpan<Result<TValue>> values)
            where TKey : notnull;

        void BatchError<TKey>(
            IActivityScope scope,
            IReadOnlyList<TKey> keys,
            Exception error);

        void BatchItemError<TKey>(
            IActivityScope scope,
            TKey key,
            Exception error);
    }
}
