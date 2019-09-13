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
    {
        private readonly object _sync = new object();
        private readonly CancellationTokenSource _disposeTokenSource =
            new CancellationTokenSource();
        private bool _disposed;
        private TaskCompletionBuffer<TKey, TValue> _buffer;
        private ITaskCache<TValue> _cache;
        private readonly CacheKeyResolverDelegate<TKey> _cacheKeyResolver;
        private AutoResetEvent _delaySignal;
        private DataLoaderOptions<TKey> _options;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoaderBase{TKey, TValue}"/> class.
        /// </summary>
        protected DataLoaderBase()
            : this(new DataLoaderOptions<TKey>())
        { }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoaderBase{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="cache">
        /// A cache instance for <c>Tasks</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="cache"/> is <c>null</c>.
        /// </exception>
        protected DataLoaderBase(ITaskCache<TValue> cache)
            : this(new DataLoaderOptions<TKey>(), cache)
        { }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoaderBase{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="options">
        /// A configuration for <c>DataLoaders</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="options"/> is <c>null</c>.
        /// </exception>
        protected DataLoaderBase(DataLoaderOptions<TKey> options)
            : this(options, new TaskCache<TValue>(
                options?.CacheSize ?? Defaults.CacheSize,
                options?.SlidingExpiration ??
                    Defaults.SlidingExpiration))
        { }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="DataLoaderBase{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="options">
        /// A configuration for <c>DataLoaders</c>.
        /// </param>
        /// <param name="cache">
        /// A cache instance for <c>Tasks</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="options"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Throws if <paramref name="cache"/> is <c>null</c>.
        /// </exception>
        protected DataLoaderBase(DataLoaderOptions<TKey> options,
            ITaskCache<TValue> cache)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _buffer = new TaskCompletionBuffer<TKey, TValue>();
            _cache = cache ??
                throw new ArgumentNullException(nameof(cache));
            _cacheKeyResolver = _options.CacheKeyResolver ??
                ((TKey key) => key);

            StartAsyncBackgroundDispatching();
        }

        /// <inheritdoc />
        public event RequestBufferedEventHandler RequestBuffered;

        /// <inheritdoc />
        public int BufferedRequests => _buffer.Count;

        /// <inheritdoc />
        public int CachedValues => _cache.Usage;

        #region Explicit Implementation of IDataLoader

        /// <inheritdoc />
        Task<object> IDataLoader.LoadAsync(
            object key,
            CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return Task.Factory.StartNew<Task<object>>(async () =>
                await LoadAsync((TKey)key, cancellationToken)
                    .ConfigureAwait(false),
                        TaskCreationOptions.RunContinuationsAsynchronously)
                            .Unwrap();
        }

        /// <inheritdoc />
        Task<IReadOnlyList<object>> IDataLoader.LoadAsync(
            CancellationToken cancellationToken,
            params object[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            TKey[] newKeys = keys.Select(k => (TKey)k).ToArray();

            return Task.Factory.StartNew(async () =>
                (IReadOnlyList<object>)await LoadAsync(
                    newKeys,
                    cancellationToken)
                        .ConfigureAwait(false),
                            TaskCreationOptions.RunContinuationsAsynchronously)
                                .Unwrap();
        }

        /// <inheritdoc />
        Task<IReadOnlyList<object>> IDataLoader.LoadAsync(
            IReadOnlyCollection<object> keys,
            CancellationToken cancellationToken)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            TKey[] newKeys = keys.Select(k => (TKey)k).ToArray();

            return Task.Factory.StartNew(async () =>
                (IReadOnlyList<object>)await LoadAsync(
                    newKeys,
                    cancellationToken)
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
        void IDataLoader.Set(object key, Task<object> value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Task<TValue> newValue = Task.Factory.StartNew(async () =>
                (TValue)await value.ConfigureAwait(false),
                    TaskCreationOptions.RunContinuationsAsynchronously)
                        .Unwrap();

            Set((TKey)key, newValue);
        }

        #endregion

        /// <inheritdoc />
        public void Clear()
        {
            if (_options.Caching)
            {
                _cache.Clear();
            }
        }

        /// <inheritdoc />
        public async Task DispatchAsync(CancellationToken cancellationToken)
        {
            if (_options.Batching)
            {
                if (_options.AutoDispatching)
                {
                    // this line is for interrupting the delay to wait before
                    // auto dispatching sends the next batch. this is like an
                    // implicit dispatch.
                    _delaySignal.Set();
                }
                else
                {
                    CancellationToken combinedToken = _disposeTokenSource
                        .CreateLinkedCancellationToken(cancellationToken);

                    // this is the way for doing a explicit dispatch when auto
                    // dispatching is disabled.
                    await DispatchBatchAsync(combinedToken)
                        .ConfigureAwait(false);
                }
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
        protected abstract Task<IReadOnlyList<Result<TValue>>> FetchAsync(
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken);

        /// <inheritdoc />
        public Task<TValue> LoadAsync(
            TKey key,
            CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            lock (_sync)
            {
                object cacheKey = _cacheKeyResolver(key);

                if (_options.Caching && _cache.TryGetValue(
                    cacheKey,
                    out Task<TValue> cachedValue))
                {
                    DiagnosticEvents.ReceivedValueFromCache(
                        key,
                        cacheKey,
                        cachedValue);

                    return cachedValue;
                }

                var promise = new TaskCompletionSource<TValue>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                if (_options.Batching)
                {
                    if (!_buffer.TryAdd(key, promise) &&
                        _buffer.TryGetValue(
                            key,
                            out TaskCompletionSource<TValue> value))
                    {
                        promise.TrySetCanceled();
                        promise = value;
                    }
                    else
                    {
                        RaiseRequestBuffered();
                    }
                }
                else
                {
                    CancellationToken combinedToken = _disposeTokenSource
                        .CreateLinkedCancellationToken(cancellationToken);

                    // must run decoupled from this task, so that LoadAsync
                    // responds immediately; do not await here.
                    Task.Factory.StartNew(
                        () => DispatchSingleAsync(
                            key,
                            promise,
                            combinedToken),
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }

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
            IDictionary<TKey, TaskCompletionSource<TValue>> bufferedPromises,
            IReadOnlyList<TKey> keys,
            Exception error)
        {
            DiagnosticEvents.ReceivedBatchError(keys, error);

            for (var i = 0; i < keys.Count; i++)
            {
                object cacheKey = _cacheKeyResolver(keys[i]);

                bufferedPromises[keys[i]].SetException(error);
                _cache.Remove(cacheKey);
            }
        }

        private void BatchOperationSucceeded(
            IDictionary<TKey, TaskCompletionSource<TValue>> bufferedPromises,
            IReadOnlyList<TKey> keys,
            IReadOnlyList<Result<TValue>> results)
        {
            if (keys.Count == results.Count)
            {
                for (var i = 0; i < keys.Count; i++)
                {
                    SetSingleResult(bufferedPromises[keys[i]],
                        keys[i], results[i]);
                }
            }
            else
            {
                // in case we got here less or more results as expected, the
                // complete batch operation failed.

                Exception error = Errors.CreateKeysAndValuesMustMatch(
                    keys.Count, results.Count);

                BatchOperationFailed(bufferedPromises, keys, error);
            }
        }

        private TaskCompletionBuffer<TKey, TValue> CopyAndClearBuffer()
        {
            TaskCompletionBuffer<TKey, TValue> copy = _buffer;

            _buffer = new TaskCompletionBuffer<TKey, TValue>();

            return copy;
        }

        private Task DispatchBatchAsync(CancellationToken cancellationToken)
        {
            return _sync.LockAsync(
                () => !_buffer.IsEmpty,
                async () =>
                {
                    TaskCompletionBuffer<TKey, TValue> copy =
                        CopyAndClearBuffer();
                    TKey[] keys = copy.Keys.ToArray();

                    if (_options.MaxBatchSize > 0 &&
                        copy.Count > _options.MaxBatchSize)
                    {
                        // splits items from buffer into chunks and instead of
                        // sending the complete buffer, it sends chunk by chunk
                        var chunkSize = (int)Math.Ceiling(
                            (decimal)copy.Count / _options.MaxBatchSize);

                        for (var i = 0; i < chunkSize; i++)
                        {
                            TKey[] chunkedKeys = keys
                                .Skip(i * _options.MaxBatchSize)
                                .Take(_options.MaxBatchSize)
                                .ToArray();

                            await FetchInternalAsync(
                                copy,
                                chunkedKeys,
                                cancellationToken)
                                    .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // sends all items from the buffer in one batch
                        // operation
                        await FetchInternalAsync(
                            copy,
                            keys,
                            cancellationToken)
                                .ConfigureAwait(false);
                    }
                });
        }

        private async Task DispatchSingleAsync(
            TKey key,
            TaskCompletionSource<TValue> promise,
            CancellationToken cancellationToken)
        {
            var keys = new TKey[] { key };
            Activity activity = DiagnosticEvents
                .StartSingle(key);
            IReadOnlyList<Result<TValue>> results =
                await FetchAsync(keys, cancellationToken)
                    .ConfigureAwait(false);

            if (results.Count == 1)
            {
                SetSingleResult(promise, key, results.First());
            }
            else
            {
                Exception error = Errors.CreateKeysAndValuesMustMatch(1,
                    results.Count);

                DiagnosticEvents.ReceivedError(key, error);
                promise.SetException(error);
            }

            DiagnosticEvents.StopSingle(activity, key,
                results.Select(r => r.Value).ToArray());
        }

        private async Task FetchInternalAsync(
            IDictionary<TKey, TaskCompletionSource<TValue>> bufferedPromises,
            IReadOnlyList<TKey> keys,
            CancellationToken cancellationToken)
        {
            Activity activity = DiagnosticEvents
                .StartBatching(keys);
            IReadOnlyList<Result<TValue>> results = new Result<TValue>[0];

            try
            {
                results = await FetchAsync(keys, cancellationToken)
                    .ConfigureAwait(false);
                BatchOperationSucceeded(bufferedPromises, keys,
                    results);
            }
            catch (Exception ex)
            {
                BatchOperationFailed(bufferedPromises, keys, ex);
            }

            DiagnosticEvents.StopBatching(activity, keys,
                results.Select(r => r.Value).ToArray());
        }

        private async Task<IReadOnlyList<TValue>> LoadInternalAsync(
            TKey[] keys,
            CancellationToken cancellationToken)
        {
            return await Task.WhenAll(keys.Select(k =>
                LoadAsync(k, cancellationToken))).ConfigureAwait(false);
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

        private void RaiseRequestBuffered()
        {
            RequestBuffered?.Invoke(this, EventArgs.Empty);
        }

        private static void SetSingleResult(
            TaskCompletionSource<TValue> promise,
            TKey key,
            Result<TValue> result)
        {
            if (result.IsError)
            {
                DiagnosticEvents.ReceivedError(key, result);
                promise.SetException(result);
            }
            else
            {
                promise.SetResult(result);
            }
        }

        private void StartAsyncBackgroundDispatching()
        {
            if (_options.AutoDispatching && _options.Batching)
            {
                // here we removed the lock because we take care that this
                // function is called once within the constructor.

                _delaySignal = new AutoResetEvent(true);

                Task.Factory.StartNew(async () =>
                {
                    while (!_disposeTokenSource.IsCancellationRequested)
                    {
                        _delaySignal.WaitOne(_options.BatchRequestDelay);
                        await DispatchBatchAsync(_disposeTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        #region IDisposable

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
                    _delaySignal?.Set();
                    (_cache as IDisposable).Dispose();
                    _disposeTokenSource.Dispose();
                    _delaySignal?.Dispose();
                }

                _buffer = null;
                _cache = null;
                _delaySignal = null;
                _options = null;

                _disposed = true;
            }
        }

        #endregion
    }
}
