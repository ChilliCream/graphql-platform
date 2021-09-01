using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenDonut
{
    public class DataLoaderDiagnosticEventListener : IDataLoaderDiagnosticEvents
    {
        /// <summary>
        /// A no-op <see cref="IActivityScope"/> that can be returned from
        /// event methods that are not interested in when the scope is disposed.
        /// </summary>
        protected static IActivityScope EmptyScope { get; } = new EmptyActivityScope();

        /// <inheritdoc />
        public virtual void ResolvedTaskFromCache(
            TaskCacheKey cacheKey,
            Task task)
        { }

        /// <inheritdoc />
        public virtual IActivityScope ExecuteBatch<TKey>(
            IDataLoader dataLoader,
            IReadOnlyList<TKey> keys)
            => EmptyScope;

        /// <inheritdoc />
        public void BatchResults<TKey, TValue>(
            IActivityScope scope,
            IReadOnlyList<TKey> keys,
            ReadOnlySpan<Result<TValue>> values)
            where TKey : notnull
        { }

        /// <inheritdoc />
        public void BatchError<TKey>(
            IActivityScope scope,
            IReadOnlyList<TKey> keys,
            Exception error)
        { }

        /// <inheritdoc />
        public void BatchItemError<TKey>(
            IActivityScope scope,
            TKey key,
            Exception error)
        { }

        private sealed class EmptyActivityScope : IActivityScope
        {
            public void Dispose()
            {
            }
        }
    }
}
