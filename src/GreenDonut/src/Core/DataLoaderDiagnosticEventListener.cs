using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenDonut
{
    /// <summary>
    /// A base class to create a DataLoader diagnostic event listener.
    /// </summary>
    public class DataLoaderDiagnosticEventListener : IDataLoaderDiagnosticEventListener
    {
        /// <summary>
        /// A no-op <see cref="IActivityScope"/> that can be returned from
        /// event methods that are not interested in when the scope is disposed.
        /// </summary>
        protected static IDisposable EmptyScope { get; } = new EmptyActivityScope();

        /// <inheritdoc />
        public virtual void ResolvedTaskFromCache(
            IDataLoader dataLoader,
            TaskCacheKey cacheKey,
            Task task)
        { }

        /// <inheritdoc />
        public virtual IDisposable ExecuteBatch<TKey>(
            IDataLoader dataLoader,
            IReadOnlyList<TKey> keys)
            => EmptyScope;

        /// <inheritdoc />
        public virtual void BatchResults<TKey, TValue>(
            IReadOnlyList<TKey> keys,
            ReadOnlySpan<Result<TValue>> values)
            where TKey : notnull
        { }

        /// <inheritdoc />
        public virtual void BatchError<TKey>(
            IReadOnlyList<TKey> keys,
            Exception error)
        { }

        /// <inheritdoc />
        public virtual void BatchItemError<TKey>(
            TKey key,
            Exception error)
        { }

        private sealed class EmptyActivityScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
