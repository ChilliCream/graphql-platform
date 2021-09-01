using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut
{
    /// <summary>
    /// A <c>DataLoader</c> creates a public API for loading data from a
    /// particular data back-end with unique keys such as the `id` column of a
    /// SQL table or document name in a MongoDB database, given a batch loading
    /// function. -- facebook
    ///
    /// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
    /// caution when used in long-lived applications or those which serve many
    /// users with different access permissions and consider creating a new
    /// instance per web request. -- facebook
    ///
    /// This is an abstraction for all kind of <c>DataLoaders</c>.
    /// </summary>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TValue">A value type.</typeparam>
    public abstract partial class DataLoaderBase<TKey, TValue>
        : IDataLoader<TKey, TValue>
        , IDisposable
        where TKey : notnull
    {
        private readonly object _sync = new();
        private readonly CancellationTokenSource _disposeTokenSource = new();
        private readonly IBatchScheduler _batchScheduler;
        private readonly CacheKeyResolverDelegate _cacheKeyResolver;
        private readonly int _maxBatchSize;
        private readonly ITaskCache? _cache;
        private readonly IDataLoaderDiagnosticEvents _diagnosticEvents;
        private readonly DataLoaderOptions _options;
        private Batch<TKey, TValue>? _currentBatch;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoader{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="batchScheduler">
        /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
        /// </param>
        /// <param name="options">
        /// An options object to configure the behavior of this particular
        /// <see cref="DataLoader{TKey, TValue}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="options"/> is <c>null</c>.
        /// </exception>
        protected DataLoaderBase(IBatchScheduler batchScheduler, DataLoaderOptions? options = null)
        {
            _options = options ?? new DataLoaderOptions();
            _cache = _options.Cache;
            _cacheKeyResolver = _options.CacheKeyResolver;
            _batchScheduler = batchScheduler;
            _maxBatchSize = _options.GetBatchSize();
        }

        /// <inheritdoc />
        public Task<TValue> LoadAsync(TKey key, CancellationToken cancellationToken)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var cached = true;
            object cacheKey = _cacheKeyResolver(key, this, typeof(TValue));

            lock (_sync)
            {
                if (_cache is not null)
                {
                    var cachedTask = (Task<TValue>)_cache.GetOrSetValue(cacheKey, CreatePromise);

                    if (cached)
                    {
                        _diagnosticEvents.ReceivedValueFromCache(key, cacheKey, cachedTask);
                    }

                    return cachedTask;
                }

                return CreatePromise();
            }

            Task<TValue> CreatePromise()
            {
                cached = false;
                return GetOrCreatePromiseUnsafe(key).Task;
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TValue>> LoadAsync(
            IReadOnlyCollection<TKey> keys,
            CancellationToken cancellationToken)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var index = 0;
            var tasks = new Task<TValue>[keys.Count];
            bool cached;
            TKey currentKey;

            lock (_sync)
            {
                if (_cache is not null)
                {
                    InitializeWithCache();
                }
                else
                {
                    Initialize();
                }
            }

            return WhenAll();

            void InitializeWithCache()
            {
                foreach (TKey key in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    cached = true;
                    currentKey = key;
                    object cacheKey = _cacheKeyResolver(key, this, typeof(TValue));

                    var cachedTask = (Task<TValue>)_cache.GetOrSetValue(cacheKey, CreatePromise);

                    if (cached)
                    {
                        _diagnosticEvents.ReceivedValueFromCache(key, cacheKey, cachedTask);
                    }

                    tasks[index++] = cachedTask;
                }
            }

            void Initialize()
            {
                foreach (TKey key in keys)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    currentKey = key;
                    tasks[index++] = CreatePromise();
                }
            }

            async Task<IReadOnlyList<TValue>> WhenAll()
                => await Task.WhenAll(tasks).ConfigureAwait(false);

            Task<TValue> CreatePromise()
            {
                cached = false;
                return GetOrCreatePromiseUnsafe(currentKey).Task;
            }
        }

        /// <inheritdoc />
        public void Remove(TKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_cache is not null)
            {
                object cacheKey = _cacheKeyResolver(key, this, typeof(TValue));
                _cache.Remove(cacheKey);
            }
        }

        /// <inheritdoc />
        public void Set(TKey key, Task<TValue> value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (_cache is not null)
            {
                object cacheKey = _cacheKeyResolver(key, this, typeof(TValue));
                _cache.TryAdd(cacheKey, value);
            }
        }

        private void BatchOperationFailed(
            Batch<TKey, TValue> batch,
            IReadOnlyList<TKey> keys,
            Exception error)
        {
            DiagnosticEvents.ReceivedBatchError(keys, error);

            for (var i = 0; i < keys.Count; i++)
            {
                if (_cache is not null)
                {
                    object cacheKey = _cacheKeyResolver(keys[i], this, typeof(TValue));
                    _cache.Remove(cacheKey);
                }

                batch.Get(keys[i]).SetException(error);
            }
        }

        private void BatchOperationSucceeded(
            Batch<TKey, TValue> batch,
            IReadOnlyList<TKey> keys,
            IReadOnlyList<Result<TValue>> results)
        {
            if (keys.Count == results.Count)
            {
                for (var i = 0; i < keys.Count; i++)
                {
                    SetSingleResult(batch.Get(keys[i]), keys[i], results[i]);
                }
            }
            else
            {
                // in case we got here less or more results as expected, the
                // complete batch operation failed.
                Exception error = Errors.CreateKeysAndValuesMustMatch(keys.Count, results.Count);

                BatchOperationFailed(batch, keys, error);
            }
        }

        private ValueTask DispatchBatchAsync(
            Batch<TKey, TValue> batch,
            CancellationToken cancellationToken)
        {
            return batch.StartDispatchingAsync(async () =>
            {
                Activity? activity = DiagnosticEvents.StartBatching(batch.Keys);
                IReadOnlyList<Result<TValue>> results = Array.Empty<Result<TValue>>();

                try
                {
                    results = await FetchAsync(batch.Keys, cancellationToken).ConfigureAwait(false);
                    BatchOperationSucceeded(batch, batch.Keys, results);
                }
                catch (Exception ex)
                {
                    BatchOperationFailed(batch, batch.Keys, ex);
                }

                DiagnosticEvents.StopBatching(activity, batch.Keys,
                    results.Select(result => result.Value).ToArray());
            });
        }

        private TaskCompletionSource<TValue> GetOrCreatePromiseUnsafe(TKey key)
        {
            if (_currentBatch is not null &&
                _currentBatch.Size < _maxBatchSize &&
                _currentBatch.TryGetOrCreate(key, out TaskCompletionSource<TValue>? promise))
            {
                return promise;
            }

            var newBatch = new Batch<TKey, TValue>();

            newBatch.TryGetOrCreate(key, out TaskCompletionSource<TValue>? newPromise);
            _batchScheduler.Schedule(() => DispatchBatchAsync(newBatch, _disposeTokenSource.Token));
            _currentBatch = newBatch;

            return newPromise!;
        }

        private static void SetSingleResult(
            TaskCompletionSource<TValue> promise,
            TKey key,
            Result<TValue> result)
        {
            if (result.IsError)
            {
                DiagnosticEvents.ReceivedError(key, result!);
                promise.SetException(result!);
            }
            else
            {
                promise.SetResult(result);
            }
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing,
        /// or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Clear();
                    _disposeTokenSource.Cancel();
                    _disposeTokenSource.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
