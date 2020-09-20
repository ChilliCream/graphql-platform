using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public abstract class DataLoaderBase<TKey, TValue>
        : IDataLoader<TKey, TValue>
        , IDisposable
        where TKey : notnull
    {
        private readonly CancellationTokenSource _disposeTokenSource = new CancellationTokenSource();
        private readonly object _sync = new object();
        private readonly IBatchScheduler _batchScheduler;
        private readonly CacheKeyResolverDelegate<TKey> _cacheKeyResolver;
        private readonly int _maxBatchSize;
        private ITaskCache _cache;
        private Batch<TKey, TValue>? _currentBatch;
        private bool _disposed;
        private DataLoaderOptions<TKey> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataLoader{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="batchScheduler">
        /// A scheduler to tell the <c>DataLoader</c> when to dispatch buffered batches.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="options"/> is <c>null</c>.
        /// </exception>
        protected DataLoaderBase(IBatchScheduler batchScheduler)
            : this(batchScheduler, null)
        { }

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
        protected DataLoaderBase(IBatchScheduler batchScheduler, DataLoaderOptions<TKey>? options)
        {
            _options = options ?? new DataLoaderOptions<TKey>();
            _cache = _options.Cache ?? new TaskCache(_options.CacheSize);
            _cacheKeyResolver = _options.CacheKeyResolver ?? ((TKey key) => key);
            _batchScheduler = batchScheduler;
            _maxBatchSize = _options.GetBatchSize();
        }

        /// <inheritdoc />
        Task<object?> IDataLoader.LoadAsync(
            object key,
            CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Task.Factory.StartNew<Task<object?>>(
                async () => await LoadAsync((TKey)key, cancellationToken).ConfigureAwait(false),
                TaskCreationOptions.RunContinuationsAsynchronously)
                    .Unwrap();
        }

        /// <inheritdoc />
        Task<IReadOnlyList<object?>> IDataLoader.LoadAsync(
            CancellationToken cancellationToken,
            params object[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            TKey[] newKeys = keys.Select(key => (TKey)key).ToArray();

            return Task.Factory.StartNew(
                async () => (IReadOnlyList<object?>)await LoadAsync(newKeys, cancellationToken)
                    .ConfigureAwait(false),
                TaskCreationOptions.RunContinuationsAsynchronously)
                    .Unwrap();
        }

        /// <inheritdoc />
        Task<IReadOnlyList<object?>> IDataLoader.LoadAsync(
            IReadOnlyCollection<object> keys,
            CancellationToken cancellationToken)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            TKey[] newKeys = keys.Select(key => (TKey)key).ToArray();

            return Task.Factory.StartNew(
                async () => (IReadOnlyList<object?>)await LoadAsync(newKeys, cancellationToken)
                    .ConfigureAwait(false),
                TaskCreationOptions.RunContinuationsAsynchronously)
                    .Unwrap();
        }

        /// <inheritdoc />
        void IDataLoader.Remove(object key)
        {
            Remove((TKey)key);
        }

        /// <inheritdoc />
        void IDataLoader.Set(object key, Task<object?> value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Task<TValue> newValue = Task.Factory.StartNew(
                async () => ((TValue)await value.ConfigureAwait(false) ?? default)!,
                TaskCreationOptions.RunContinuationsAsynchronously)
                    .Unwrap();

            Set((TKey)key, newValue);
        }

        /// <inheritdoc />
        public void Clear()
        {
            if (_options.Caching)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// A batch loading function which has to be implemented for each
        /// individual <c>DataLoader</c>. For every provided key must be a
        /// result returned. Also to be mentioned is, the results must be
        /// returned in the exact same order the keys were provided.
        /// </summary>
        /// <param name="keys">A list of keys.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A list of results which are in the exact same order as the provided
        /// keys.
        /// </returns>
        protected abstract ValueTask<IReadOnlyList<Result<TValue>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken);

        /// <inheritdoc />
        public Task<TValue> LoadAsync(TKey key, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            lock (_sync)
            {
                object cacheKey = _cacheKeyResolver(key);

                if (_options.Caching && _cache.TryGetValue(cacheKey, out object? cachedValue))
                {
                    var cachedTask = (Task<TValue>)cachedValue;

                    DiagnosticEvents.ReceivedValueFromCache(key, cacheKey, cachedTask);

                    return cachedTask;
                }

                TaskCompletionSource<TValue> promise = GetOrCreatePromise(key);

                if (_options.Caching)
                {
                    _cache.TryAdd(cacheKey, promise.Task);
                }

                return promise.Task;
            }
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TValue>> LoadAsync(
            CancellationToken cancellationToken,
            params TKey[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            return LoadInternalAsync(keys, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<TValue>> LoadAsync(
            IReadOnlyCollection<TKey> keys,
            CancellationToken cancellationToken)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            return LoadInternalAsync(keys, cancellationToken);
        }

        /// <inheritdoc />
        public void Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_options.Caching)
            {
                object cacheKey = _cacheKeyResolver(key);

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

            if (_options.Caching)
            {
                object cacheKey = _cacheKeyResolver(key);

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
                object cacheKey = _cacheKeyResolver(keys[i]);

                _cache.Remove(cacheKey);
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
                IReadOnlyList<Result<TValue>> results = new Result<TValue>[0];

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

        private TaskCompletionSource<TValue> GetOrCreatePromise(TKey key)
        {
            if (_currentBatch is {} &&
                _currentBatch.Size < _maxBatchSize &&
                _currentBatch.TryGetOrCreate(key, out TaskCompletionSource<TValue>? promise) &&
                promise is {})
            {
                return promise;
            }

            var newBatch = new Batch<TKey, TValue>();

            newBatch.TryGetOrCreate(key, out TaskCompletionSource<TValue>? newPromise);
            _batchScheduler.Schedule(() =>
                DispatchBatchAsync(newBatch, _disposeTokenSource.Token));
            _currentBatch = newBatch;

            return newPromise!;
        }

        private async Task<IReadOnlyList<TValue>> LoadInternalAsync(
            TKey[] keys,
            CancellationToken cancellationToken)
        {
            return await Task.WhenAll(keys.Select(key =>
                LoadAsync(key, cancellationToken))).ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<TValue>> LoadInternalAsync(
            IReadOnlyCollection<TKey> keys,
            CancellationToken cancellationToken)
        {
            var index = 0;
            var tasks = new Task<TValue>[keys.Count];

            foreach (TKey key in keys)
            {
                tasks[index++] = LoadAsync(key, cancellationToken);
            }

            return await Task.WhenAll(tasks).ConfigureAwait(false);
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
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
