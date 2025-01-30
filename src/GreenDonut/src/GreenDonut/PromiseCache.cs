using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace GreenDonut;

/// <summary>
/// A memorization cache for <c>DataLoader</c>.
/// </summary>
public sealed class PromiseCache : IPromiseCache
{
    private const int _minimumSize = 10;
    private readonly object _sync = new();
    private readonly ConcurrentDictionary<PromiseCacheKey, Entry> _map = new();
    private readonly ConcurrentDictionary<Type, ImmutableArray<Subscription>> _subscriptions = new();
    private readonly List<IPromise> _promises = new();
    private readonly int _size;
    private readonly int _order;
    private List<(PromiseCacheKey? Key, IPromise Promise)>? _buffer;
    private int _usage;
    private Entry? _head;

    /// <summary>
    /// Creates a new instance of <see cref="PromiseCache"/>.
    /// </summary>
    /// <param name="size">
    /// The size of the cache. The minimum cache size is 10.
    /// </param>
    public PromiseCache(int size)
    {
        _size = size < _minimumSize ? _minimumSize : size;
        _order = Convert.ToInt32(size * 0.9);
    }

    /// <inheritdoc />
    public int Size => _size;

    /// <inheritdoc />
    public int Usage => _usage;

    /// <inheritdoc />
    public Task<T> GetOrAddTask<T>(PromiseCacheKey key, Func<PromiseCacheKey, Promise<T>> createPromise)
    {
        if (key.Type is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (createPromise is null)
        {
            throw new ArgumentNullException(nameof(createPromise));
        }

        var read = true;

        var entry = _map.GetOrAdd(key, k =>
        {
            read = false;
            return AddNewEntry(k, createPromise(k));
        });

        if (read)
        {
            TouchEntryUnsafe(entry);
        }

        var promise = (Promise<T>)entry.Value;

        if (!promise.IsClone)
        {
            promise.OnComplete(NotifySubscribers, new CacheAndKey(this, key));
        }

        return promise.Task;
    }

    /// <inheritdoc />
    public bool TryAdd<T>(PromiseCacheKey key, Promise<T> promise)
    {
        if (key.Type is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (promise.Task is null)
        {
            throw new ArgumentNullException(nameof(promise));
        }

        var read = true;

        _map.GetOrAdd(key, k =>
        {
            read = false;
            return AddNewEntry(k, promise);
        });

        if (!promise.IsClone)
        {
            promise.OnComplete(NotifySubscribers, new CacheAndKey(this, key));
        }

        return !read;
    }

    /// <inheritdoc />
    public bool TryAdd<T>(PromiseCacheKey key, Func<Promise<T>> createPromise)
    {
        if (key.Type is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (createPromise is null)
        {
            throw new ArgumentNullException(nameof(createPromise));
        }

        var read = true;

        var entry = _map.GetOrAdd(key, k =>
        {
            read = false;
            return AddNewEntry(k, createPromise());
        });

        var promise = (Promise<T>)entry.Value;

        if (!promise.IsClone)
        {
            promise.OnComplete(NotifySubscribers, new CacheAndKey(this, key));
        }

        return !read;
    }

    /// <inheritdoc />
    public bool TryRemove(PromiseCacheKey key)
    {
        if (_map.TryRemove(key, out var entry))
        {
            lock (_sync)
            {
                RemoveEntryUnsafe(entry);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Publish<T>(T value)
    {
        var promise = Promise<T>.Create(value, cloned: true);

        lock (_promises)
        {
            _promises.Add(promise);
        }

        if (_subscriptions.TryGetValue(typeof(T), out var subscriptions))
        {
            foreach (var subscription in subscriptions)
            {
                if (subscription is Subscription<T> casted)
                {
                    casted.OnNext(promise);
                }
            }
        }
    }

    /// <inheritdoc />
    public void PublishMany<T>(ReadOnlySpan<T> values)
    {
        var buffer = ArrayPool<Promise<T>>.Shared.Rent(values.Length);
        var bufferedPromises = buffer.AsSpan().Slice(0, values.Length);

        // first we add the promises tp the promise list, which we keep
        // for DataLoader that are not instantiated yet.
        // for this we need a quick lock as the list is shared.
        // once a DataLoader instantiate it can replay all promises that
        // were published before.
        lock (_promises)
        {
            for (var i = 0; i < values.Length; i++)
            {
                var promise = Promise<T>.Create(values[i], cloned: true);
                bufferedPromises[i] = promise;
                _promises.Add(promise);
            }
        }

        // now we notify all subscribers that are interested in the current promise type.
        if (_subscriptions.TryGetValue(typeof(T), out var subscriptions))
        {
            foreach (var subscription in subscriptions)
            {
                if (subscription is Subscription<T> casted)
                {
                    for (var i = 0; i < bufferedPromises.Length; i++)
                    {
                        casted.OnNext(bufferedPromises[i]);
                    }
                }
            }
        }

        // last we clear the buffer and return it to the pool.
        bufferedPromises.Clear();
        ArrayPool<Promise<T>>.Shared.Return(buffer);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(
        Action<IPromiseCache, Promise<T>> next,
        string? skipCacheKeyType)
    {
        var type = typeof(T);
        var promises = Interlocked.Exchange(ref _buffer, null) ?? [];
        var subscription = new Subscription<T>(this, next, skipCacheKeyType);

        _subscriptions.AddOrUpdate(
            type,
            _ => ImmutableArray.Create<Subscription>(subscription),
            (_, list) => list.Add(subscription));

        lock (_promises)
        {
            foreach (var promise in _promises)
            {
                if (promise is Promise<T>)
                {
                    promises.Add((null, promise));
                }
            }
        }

        lock (_sync)
        {
            var first = _head;
            var current = first;

            while (current is not null)
            {
                if (current.Value.Task.IsCompletedSuccessfully
                    && !current.Value.IsClone
                    && current.Value.Type == type)
                {
                    promises.Add((current.Key, current.Value.Clone()));
                }

                current = current.Next;

                if (current == first)
                {
                    break;
                }
            }
        }

        foreach (var entry in promises)
        {
            if (entry.Key.HasValue)
            {
                subscription.OnNext(entry.Key.Value, (Promise<T>)entry.Promise);
            }
            else
            {
                subscription.OnNext((Promise<T>)entry.Promise);
            }
        }

        promises.Clear();
        _buffer = Interlocked.CompareExchange(ref _buffer, promises, null);

        return subscription;
    }

    private void NotifySubscribers<T>(PromiseCacheKey key, Promise<T> promise)
    {
        if (_subscriptions.TryGetValue(typeof(T), out var subscriptions))
        {
            promise = promise.Clone();

            foreach (var subscription in subscriptions)
            {
                if (subscription is Subscription<T> casted)
                {
                    casted.OnNext(key, promise);
                }
            }
        }
    }

    private static void NotifySubscribers<T>(Promise<T> promise, CacheAndKey state)
        => state.Cache.NotifySubscribers(state.Key, promise);

    /// <inheritdoc />
    public void Clear()
    {
        lock (_sync)
        {
            var current = _head;

            if (current is not null)
            {
                do
                {
                    current.Value.TryCancel();
                    current = current.Next;
                } while (current is not null && !ReferenceEquals(_head, current));
            }

            _map.Clear();
            _buffer?.Clear();
            _subscriptions.Clear();
            _promises.Clear();
            _head = null;
            _usage = 0;
        }
    }

    private Entry AddNewEntry(PromiseCacheKey key, IPromise promise)
    {
        lock (_sync)
        {
            var entry = new Entry(key, promise);
            AppendEntryUnsafe(entry);
            ClearSpaceForNewEntryUnsafe();
            return entry;
        }
    }

    private void ClearSpaceForNewEntryUnsafe()
    {
        while (_head is not null && _usage > _size)
        {
            var last = _head.Previous!;
            RemoveEntryUnsafe(last);
            _map.TryRemove(last.Key, out _);
        }
    }

    private void TouchEntryUnsafe(Entry touched)
    {
        if (_order > _usage || _head == touched)
        {
            return;
        }

        lock (_sync)
        {
            if (RemoveEntryUnsafe(touched))
            {
                AppendEntryUnsafe(touched);
            }
        }
    }

    private void AppendEntryUnsafe(Entry newEntry)
    {
        if (_head is not null)
        {
            newEntry.Next = _head;
            newEntry.Previous = _head.Previous;
            _head.Previous!.Next = newEntry;
            _head.Previous = newEntry;
            _head = newEntry;
        }
        else
        {
            newEntry.Next = newEntry;
            newEntry.Previous = newEntry;
            _head = newEntry;
        }

        _usage++;
    }

    private bool RemoveEntryUnsafe(Entry entry)
    {
        if (entry.Next == null)
        {
            return false;
        }

        if (entry.Next == entry)
        {
            _head = null;
        }
        else
        {
            entry.Next!.Previous = entry.Previous;
            entry.Previous!.Next = entry.Next;

            if (_head == entry)
            {
                _head = entry.Next;
            }

            entry.Next = null;
            entry.Previous = null;
        }

        _usage--;
        return true;
    }

    private sealed class Entry(PromiseCacheKey key, IPromise value)
    {
        public readonly PromiseCacheKey Key = key;
        public readonly IPromise Value = value;
        public Entry? Next;
        public Entry? Previous;
    }

    private sealed class Subscription<T>(
        PromiseCache owner,
        Action<IPromiseCache, Promise<T>> next,
        string? skipCacheKeyType)
        : Subscription(typeof(T), owner._subscriptions)
    {
        public void OnNext(PromiseCacheKey key, Promise<T> promise)
        {
            if (promise.Task.IsCompletedSuccessfully
                && skipCacheKeyType?.Equals(key.Type, StringComparison.Ordinal) != true)
            {
                next(owner, promise);
            }
        }

        public void OnNext(Promise<T> promise)
        {
            if (promise.Task.IsCompletedSuccessfully)
            {
                next(owner, promise);
            }
        }
    }

    private abstract class Subscription(
        Type type,
        ConcurrentDictionary<Type, ImmutableArray<Subscription>> subscriptions)
        : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                subscriptions.AddOrUpdate(
                    type,
                    _ => ImmutableArray.Create(this),
                    (_, list) => list.Remove(this));

                _disposed = true;
            }
        }
    }

    private readonly struct CacheAndKey(PromiseCache cache, PromiseCacheKey key)
    {
        public PromiseCache Cache { get; } = cache;

        public PromiseCacheKey Key { get; } = key;
    }
}
