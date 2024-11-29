using System.Collections.Immutable;
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
    where TKey : notnull
{
    private readonly object _sync = new();
    private readonly IBatchScheduler _batchScheduler;
    private readonly int _maxBatchSize;
    private readonly IDataLoaderDiagnosticEvents _diagnosticEvents;
    private ImmutableDictionary<string, IDataLoader> _branches =
        ImmutableDictionary<string, IDataLoader>.Empty;
    private Batch<TKey>? _currentBatch;

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
        Cache = options.Cache;
        _batchScheduler = batchScheduler;
        _maxBatchSize = options.MaxBatchSize;
        CacheKeyType = GetCacheKeyType(GetType());
    }

    /// <summary>
    /// Gets access to the cache of this DataLoader.
    /// </summary>
    protected internal IPromiseCache? Cache { get; }

    /// <summary>
    /// Gets the cache key type for this DataLoader.
    /// </summary>
    protected internal virtual string CacheKeyType { get; }

    /// <summary>
    /// Gets or sets the context data which can be used to store
    /// transient state on the DataLoader.
    /// </summary>
    public IImmutableDictionary<string, object?> ContextData { get; set; } =
        ImmutableDictionary<string, object?>.Empty;

    /// <summary>
    /// Specifies if the values fetched by this DataLoader
    /// are propagated through the cache.
    /// </summary>
    protected virtual bool AllowCachePropagation => true;

    /// <summary>
    /// Specifies if this DataLoader allows branching.
    /// </summary>
    protected virtual bool AllowBranching => true;

    /// <summary>
    /// Gets the batch scheduler of this DataLoader.
    /// </summary>
    protected internal IBatchScheduler BatchScheduler
        => _batchScheduler;

    /// <summary>
    /// Gets the options of this DataLoader.
    /// </summary>
    protected internal DataLoaderOptions Options
        => new() { MaxBatchSize = _maxBatchSize, Cache = Cache, DiagnosticEvents = _diagnosticEvents };

    /// <inheritdoc />
    public Task<TValue?> LoadAsync(
        TKey key,
        CancellationToken cancellationToken = default)
        => LoadAsync(key, CacheKeyType, AllowCachePropagation, cancellationToken);

    private Task<TValue?> LoadAsync(
        TKey key,
        string cacheKeyType,
        bool allowCachePropagation,
        CancellationToken ct)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var cached = true;
        PromiseCacheKey cacheKey = new(cacheKeyType, key);

        lock (_sync)
        {
            if (Cache is null)
            {
                return CreatePromise().Task;
            }

            var cachedTask = Cache.GetOrAddTask(cacheKey, _ => CreatePromise());

            if (cached)
            {
                _diagnosticEvents.ResolvedTaskFromCache(this, cacheKey, cachedTask);
            }

            return cachedTask;
        }

        Promise<TValue?> CreatePromise()
        {
            cached = false;
            return GetOrCreatePromiseUnsafe(
                key,
                allowCachePropagation,
                scheduleOnNewBatch: true,
                ct);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TValue?>> LoadAsync(
        IReadOnlyCollection<TKey> keys,
        CancellationToken cancellationToken = default)
        => LoadAsync(keys, CacheKeyType, AllowCachePropagation, cancellationToken);

    private Task<IReadOnlyList<TValue?>> LoadAsync(
        IReadOnlyCollection<TKey> keys,
        string cacheKeyType,
        bool allowCachePropagation,
        CancellationToken ct)
    {
        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        var index = 0;
        var tasks = new Task<TValue?>[keys.Count];
        bool cached;

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

            // we dispatch after everything is enqueued.
            if (_currentBatch is { IsScheduled: false })
            {
                ScheduleBatchUnsafe(_currentBatch, ct);
            }
        }

        return WhenAll();

        void InitializeWithCache()
        {
            foreach (var key in keys)
            {
                ct.ThrowIfCancellationRequested();

                cached = true;
                PromiseCacheKey cacheKey = new(cacheKeyType, key);
                var cachedTask = Cache.GetOrAddTask(cacheKey, k => CreatePromise((TKey)k.Key));

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
                ct.ThrowIfCancellationRequested();
                tasks[index++] = CreatePromise(key).Task;
            }
        }

        async Task<IReadOnlyList<TValue?>> WhenAll()
            => await Task.WhenAll(tasks).ConfigureAwait(false);

        Promise<TValue?> CreatePromise(TKey key)
        {
            cached = false;
            return GetOrCreatePromiseUnsafe(
                key,
                allowCachePropagation,
                scheduleOnNewBatch: false,
                ct);
        }
    }

    /// <inheritdoc />
    public void SetCacheEntry(TKey key, Task<TValue?> value)
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
            PromiseCacheKey cacheKey = new(CacheKeyType, key);
            Cache.TryAdd(cacheKey, new Promise<TValue?>(value));
        }
    }

    /// <inheritdoc />
    public void RemoveCacheEntry(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (Cache is not null)
        {
            PromiseCacheKey cacheKey = new(CacheKeyType, key);
            Cache.TryRemove(cacheKey);
        }
    }

    /// <inheritdoc />
    [Obsolete("Use SetCacheEntry instead.")]
    public void Set(TKey key, Task<TValue?> value)
    {
        SetCacheEntry(key, value);
    }

    /// <inheritdoc />
    [Obsolete("Use RemoveCacheEntry instead.")]
    public void Remove(TKey key)
    {
        RemoveCacheEntry(key);
    }

    /// <inheritdoc />
    public IDataLoader Branch<TState>(
        string key,
        CreateDataLoaderBranch<TKey, TValue, TState> createBranch,
        TState state)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(key));
        }

        if (createBranch == null)
        {
            throw new ArgumentNullException(nameof(createBranch));
        }

        if (!AllowBranching)
        {
            throw new InvalidOperationException(
                "Branching is not allowed for this DataLoader.");
        }

        if (!_branches.TryGetValue(key, out var branch))
        {
            lock (_sync)
            {
                if (!_branches.TryGetValue(key, out branch))
                {
                    var newBranch = createBranch(key, this, state);
                    _branches = _branches.Add(key, newBranch);
                    return newBranch;
                }
            }
        }

        return branch;
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
                PromiseCacheKey cacheKey = new(CacheKeyType, key);
                Cache.TryRemove(cacheKey);
            }

            batch.GetPromise<TValue>(key).TrySetError(error);
        }
    }

    private void BatchOperationSucceeded(
        Batch<TKey> batch,
        IReadOnlyList<TKey> keys,
        Result<TValue?>[] results)
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

            SetSingleResult(batch.GetPromise<TValue?>(key), key, value);
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
                var buffer = new Result<TValue?>[batch.Keys.Count];

                try
                {
                    var context = new DataLoaderFetchContext<TValue>(ContextData);
                    await FetchAsync(batch.Keys, buffer, context, cancellationToken).ConfigureAwait(false);
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

    // ReSharper disable InconsistentlySynchronizedField
    private Promise<TValue?> GetOrCreatePromiseUnsafe(
        TKey key,
        bool allowCachePropagation,
        bool scheduleOnNewBatch,
        CancellationToken ct)
    {
        var current = _currentBatch;

        if (current is not null)
        {
            // if the batch has space for more keys we just keep adding to it.
            if (current.Size < _maxBatchSize || _maxBatchSize == 0)
            {
                return current.GetOrCreatePromise<TValue?>(key, allowCachePropagation);
            }

            // if there is a current batch and if that current batch was not scheduled for efficiency reasons
            // we will schedule it before issuing a new batch.
            if (!current.IsScheduled)
            {
                ScheduleBatchUnsafe(current, ct);
            }
        }

        var newBatch = _currentBatch = BatchPool<TKey>.Shared.Get();
        var newPromise = newBatch.GetOrCreatePromise<TValue?>(key, allowCachePropagation);

        if (scheduleOnNewBatch)
        {
            ScheduleBatchUnsafe(newBatch, ct);
        }

        return newPromise;
    }

    private void ScheduleBatchUnsafe(Batch<TKey> batch, CancellationToken ct)
    {
        batch.IsScheduled = true;
        _batchScheduler.Schedule(() => DispatchBatchAsync(batch, ct));
    }

    private void SetSingleResult(
        Promise<TValue?> promise,
        TKey key,
        Result<TValue?> result)
    {
        if (result.Kind is ResultKind.Value)
        {
            promise.TrySetResult(result);
        }
        else
        {
            _diagnosticEvents.BatchItemError(key, result.Error!);
            promise.TrySetError(result.Error!);
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
        if (Cache is null)
        {
            return;
        }

        foreach (var item in items)
        {
            PromiseCacheKey cacheKey = new(cacheKeyType, key(item));
            Cache.TryAdd(cacheKey, () => new Promise<TV>(value(item)));
        }
    }

    /// <summary>
    /// A helper to adds another cache lookup to a resolved entity.
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
        if (Cache is null)
        {
            return;
        }

        PromiseCacheKey cacheKey = new(cacheKeyType, key);
        Cache.TryAdd(cacheKey, () => new Promise<TV>(value));
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
}
