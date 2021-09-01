using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static GreenDonut.Errors;

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
        private readonly CacheKeyFactoryDelegate _cacheKeyFactory;
        private readonly string _cacheKeyType;
        private readonly int _maxBatchSize;
        private readonly ITaskCache? _cache;
        private readonly TaskCacheOwner? _cacheOwner;
        private readonly IDataLoaderDiagnosticEvents _diagnosticEvents;
        private Batch<TKey, TValue>? _currentBatch;
        private Result<TValue>[]? _buffer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoaderBase{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="batchScheduler">
        /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
        /// </param>
        /// <param name="options">
        /// An options object to configure the behavior of this particular
        /// <see cref="DataLoaderBase{TKey, TValue}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="options"/> is <c>null</c>.
        /// </exception>
        protected DataLoaderBase(IBatchScheduler batchScheduler, DataLoaderOptions? options = null)
        {
            options ??= new DataLoaderOptions();
            _diagnosticEvents = options.DiagnosticEvents;

            if (options.Caching && options.Cache is null)
            {
                _cacheOwner = new TaskCacheOwner();
                _cache = _cacheOwner.Cache;
            }
            else
            {
                _cache = options.Cache;
            }

            _batchScheduler = batchScheduler;
            _maxBatchSize = options.MaxBatchSize;
            _cacheKeyType = options.CacheKeyTypeFactory(this);
            _cacheKeyFactory = options.CacheKeyFactory;
        }

        /// <inheritdoc />
        public Task<TValue> LoadAsync(TKey key, CancellationToken cancellationToken)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var cached = true;
            TaskCacheKey cacheKey = _cacheKeyFactory(_cacheKeyType, key);

            lock (_sync)
            {
                if (_cache is not null)
                {
                    Task<TValue> cachedTask = _cache.GetOrAddTask(cacheKey, CreatePromise);

                    if (cached)
                    {
                        _diagnosticEvents.ResolvedTaskFromCache(cacheKey, cachedTask);
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
                    TaskCacheKey cacheKey = _cacheKeyFactory(_cacheKeyType, key);

                    Task<TValue> cachedTask = _cache.GetOrAddTask(cacheKey, CreatePromise);

                    if (cached)
                    {
                        _diagnosticEvents.ResolvedTaskFromCache(cacheKey, cachedTask);
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
                TaskCacheKey cacheKey = _cacheKeyFactory(_cacheKeyType, key);
                _cache.TryRemove(cacheKey);
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
                TaskCacheKey cacheKey = _cacheKeyFactory(_cacheKeyType, key);
                _cache.TryAdd(cacheKey, value);
            }
        }

        private void BatchOperationFailed(
            Batch<TKey, TValue> batch,
            IReadOnlyList<TKey> keys,
            Exception error,
            IActivityScope scope)
        {
            _diagnosticEvents.BatchError(scope, keys, error);

            for (var i = 0; i < keys.Count; i++)
            {
                if (_cache is not null)
                {
                    TaskCacheKey cacheKey = _cacheKeyFactory(_cacheKeyType, keys[i]);
                    _cache.TryRemove(cacheKey);
                }

                batch.Get(keys[i]).SetException(error);
            }
        }

        private void BatchOperationSucceeded(
            Batch<TKey, TValue> batch,
            IReadOnlyList<TKey> keys,
            IReadOnlyList<Result<TValue>> results,
            IActivityScope scope)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                Result<TValue> value = results[i];

                if (value.Kind is ResultKind.Value)
                {
                    // in case we got here less or more results as expected, the
                    // complete batch operation failed.
                    Exception error = CreateKeysAndValuesMustMatch(keys.Count, i + 1);
                    BatchOperationFailed(batch, keys, error, scope);
                    return;
                }

                SetSingleResult(batch.Get(keys[i]), keys[i], results[i], scope);
            }
        }

        private ValueTask DispatchBatchAsync(
            Batch<TKey, TValue> batch,
            CancellationToken cancellationToken)
        {
            return batch.StartDispatchingAsync(async () =>
            {
                using IActivityScope scope = _diagnosticEvents.ExecuteBatch(this, batch.Keys);
                Result<TValue>[]? buffer = Interlocked.Exchange(ref _buffer, null);

                buffer ??= new Result<TValue>[batch.Keys.Count];

                if (buffer.Length < batch.Keys.Count)
                {
                    Array.Resize(ref buffer, batch.Keys.Count);
                }

                Memory<Result<TValue>> results = batch.Keys.Count == buffer.Length
                    ? buffer.AsMemory()
                    : buffer.AsMemory().Slice(0, batch.Keys.Count);

                try
                {
                    await FetchAsync(batch.Keys, results, cancellationToken).ConfigureAwait(false);
                    BatchOperationSucceeded(batch, batch.Keys, buffer, scope);
                    _diagnosticEvents.BatchResults<TKey, TValue>(scope, batch.Keys, results.Span);
                }
                catch (Exception ex)
                {
                    BatchOperationFailed(batch, batch.Keys, ex, scope);
                }

                results.Span.Clear();
                Interlocked.Exchange(ref _buffer, buffer);
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

        private void SetSingleResult(
            TaskCompletionSource<TValue> promise,
            TKey key,
            Result<TValue> result,
            IActivityScope scope)
        {
            if (result.Kind is ResultKind.Value)
            {
                promise.SetResult(result);
            }
            else
            {
                _diagnosticEvents.BatchItemError(scope, key, result.Error!);
                promise.SetException(result.Error!);
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
