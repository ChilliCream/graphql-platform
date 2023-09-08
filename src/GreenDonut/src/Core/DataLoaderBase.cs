using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static GreenDonut.NoopDataLoaderDiagnosticEventListener;
using static GreenDonut.Errors;

namespace GreenDonut;

/// <summary>
/// <para>
/// A <c>DataLoader</c> creates a public API for loading data from a
/// particular data back-end with unique keys such as the `id` column of a
/// SQL table or document name in a MongoDB database, given a batch loading
/// function. -- facebook
/// </para>
/// <para>
/// Each <c>DataLoader</c> instance contains a unique memoized cache. Use
/// caution when used in long-lived applications or those which serve many
/// users with different access permissions and consider creating a new
/// instance per web request. -- facebook
/// </para>
/// <para>This is an abstraction for all kind of <c>DataLoaders</c>.</para>
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
    private readonly int _maxBatchSize;
    private readonly TaskCacheOwner? _cacheOwner;
    private readonly IDataLoaderDiagnosticEvents _diagnosticEvents;
    private Batch<TKey>? _currentBatch;
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
        _diagnosticEvents = options.DiagnosticEvents ?? Default;

        if (options.Caching && options.Cache is null)
        {
            _cacheOwner = new TaskCacheOwner();
            Cache = _cacheOwner.Cache;
        }
        else
        {
            Cache = options.Caching
                ? options.Cache
                : null;
        }

        _batchScheduler = batchScheduler;
        _maxBatchSize = options.MaxBatchSize;
        CacheKeyType = GetCacheKeyType(GetType());
    }

    /// <summary>
    /// Gets access to the cache of this DataLoader.
    /// </summary>
    protected ITaskCache? Cache { get; }

    /// <summary>
    /// Gets the cache key type for this DataLoader.
    /// </summary>
    protected virtual string CacheKeyType { get; }

    /// <inheritdoc />
    public Task<TValue> LoadAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var cached = true;
        TaskCacheKey cacheKey = new(CacheKeyType, key);

        lock (_sync)
        {
            if (Cache is not null)
            {
                var cachedTask = Cache.GetOrAddTask(cacheKey, CreatePromise);

                if (cached)
                {
                    _diagnosticEvents.ResolvedTaskFromCache(this, cacheKey, cachedTask);
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
        CancellationToken cancellationToken = default)
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
            if (Cache is not null)
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
            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                cached = true;
                currentKey = key;
                TaskCacheKey cacheKey = new(CacheKeyType, key);
                var cachedTask = Cache.GetOrAddTask(cacheKey, CreatePromise);

                if (cached)
                {
                    _diagnosticEvents.ResolvedTaskFromCache(this, cacheKey, cachedTask);
                }

                tasks[index++] = cachedTask;
            }
        }

        void Initialize()
        {
            foreach (var key in keys)
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

        if (Cache is not null)
        {
            TaskCacheKey cacheKey = new(CacheKeyType, key);
            Cache.TryRemove(cacheKey);
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

        if (Cache is not null)
        {
            TaskCacheKey cacheKey = new(CacheKeyType, key);
            Cache.TryAdd(cacheKey, value);
        }
    }

    private void BatchOperationFailed(
        Batch<TKey> batch,
        IReadOnlyList<TKey> keys,
        Exception error)
    {
        _diagnosticEvents.BatchError(keys, error);

        foreach (var key in keys)
        {
            if (Cache is not null)
            {
                TaskCacheKey cacheKey = new(CacheKeyType, key);
                Cache.TryRemove(cacheKey);
            }

            batch.GetPromise<TValue>(key).TrySetException(error);
        }
    }

    private void BatchOperationSucceeded(
        Batch<TKey> batch,
        IReadOnlyList<TKey> keys,
        Result<TValue>[] results)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            var value = results[i];

            if (value.Kind is ResultKind.Undefined)
            {
                // in case we got here less or more results as expected, the
                // complete batch operation failed.
                Exception error = CreateKeysAndValuesMustMatch(keys.Count, i);
                BatchOperationFailed(batch, keys, error);
                return;
            }

            SetSingleResult(batch.GetPromise<TValue>(key), key, value);
        }
    }

    private ValueTask DispatchBatchAsync(
        Batch<TKey> batch,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_currentBatch, batch))
            {
                _currentBatch = null;
            }
        }

        return StartDispatchingAsync();

        async ValueTask StartDispatchingAsync()
        {
            var errors = false;
            
            using (_diagnosticEvents.ExecuteBatch(this, batch.Keys))
            {
                var buffer = new Result<TValue>[batch.Keys.Count];

                try
                {
                    await FetchAsync(batch.Keys, buffer, cancellationToken).ConfigureAwait(false);
                    BatchOperationSucceeded(batch, batch.Keys, buffer);
                    _diagnosticEvents.BatchResults<TKey, TValue>(batch.Keys, buffer);
                }
                catch (Exception ex)
                {
                    errors = true;
                    BatchOperationFailed(batch, batch.Keys, ex);
                }
            }

            // we return the batch here so that the keys are only cleared
            // after the diagnostic events are done.
            if (!errors)
            {
                BatchPool<TKey>.Shared.Return(batch);
            }
        }
    }

    private TaskCompletionSource<TValue> GetOrCreatePromiseUnsafe(TKey key)
    {
        if (_currentBatch is not null && _currentBatch.Size < _maxBatchSize)
        {
            return _currentBatch.GetOrCreatePromise<TValue>(key);
        }

        var newBatch = BatchPool<TKey>.Shared.Get();
        var newPromise = newBatch.GetOrCreatePromise<TValue>(key);

        // set the batch before enqueueing to avoid concurrency issues.
        _currentBatch = newBatch;
        _batchScheduler.Schedule(() => DispatchBatchAsync(newBatch, _disposeTokenSource.Token));

        return newPromise;
    }

    private void SetSingleResult(
        TaskCompletionSource<TValue> promise,
        TKey key,
        Result<TValue> result)
    {
        if (result.Kind is ResultKind.Value)
        {
            promise.SetResult(result);
        }
        else
        {
            _diagnosticEvents.BatchItemError(key, result.Error!);
            promise.SetException(result.Error!);
        }
    }

    /// <summary>
    /// A helper to add additional cache lookups to a resolved entity.
    /// </summary>
    /// <param name="cacheKeyType">
    /// The cache key type that shall be used to refer to the entity.
    /// </param>
    /// <param name="items">
    /// The items that shall be associated with other cache keys.
    /// </param>
    /// <param name="key">A delegate to create the key part.</param>
    /// <param name="value">A delegate to create the value that shall be associated.</param>
    /// <typeparam name="TItem">The item type.</typeparam>
    /// <typeparam name="TK">The key type.</typeparam>
    /// <typeparam name="TV">The value type.</typeparam>
    protected void TryAddToCache<TItem, TK, TV>(
        string cacheKeyType,
        IEnumerable<TItem> items,
        Func<TItem, TK> key,
        Func<TItem, TV> value)
        where TK : notnull
    {
        if (Cache is not null)
        {
            foreach (var item in items)
            {
                TaskCacheKey cacheKey = new(cacheKeyType, key(item));
                Cache.TryAdd(cacheKey, () => Task.FromResult(value(item)));
            }
        }
    }

    /// <summary>
    /// A helper to adds an additional cache lookup to a resolved entity.
    /// </summary>
    /// <param name="cacheKeyType">
    /// The cache key type that shall be used to refer to the entity.
    /// </param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <typeparam name="TK">The key type.</typeparam>
    /// <typeparam name="TV">The value type.</typeparam>
    protected void TryAddToCache<TK, TV>(
        string cacheKeyType,
        TK key,
        TV value)
        where TK : notnull
    {
        if (Cache is not null)
        {
            TaskCacheKey cacheKey = new(cacheKeyType, key);
            Cache.TryAdd(cacheKey, () => Task.FromResult(value));
        }
    }

    /// <summary>
    /// A helper to create a cache key type for a DataLoader.
    /// </summary>
    /// <typeparam name="TDataLoader">The DataLoader type.</typeparam>
    /// <returns>
    /// Returns the DataLoader cache key.
    /// </returns>
    protected static string GetCacheKeyType<TDataLoader>()
        where TDataLoader : IDataLoader
        => GetCacheKeyType(typeof(TDataLoader));

    /// <summary>
    /// A helper to create a cache key type for a DataLoader.
    /// </summary>
    /// <param name="type">
    /// The DataLoader type.
    /// </param>
    /// <returns>
    /// Returns the DataLoader cache key.
    /// </returns>
    protected static string GetCacheKeyType(Type type)
        => type.FullName ?? type.Name;

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
                _cacheOwner?.Dispose();
            }

            _disposed = true;
        }
    }
}
