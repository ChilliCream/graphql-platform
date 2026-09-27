namespace GreenDonut.Data.Internal;

/// <summary>
/// Demultiplexes one flat, key-ordered row stream into a <see cref="StreamPagePump{TElement}"/>
/// per requested key, and releases the source and the lifetime once every requested key's page
/// has completed or been disposed.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key that routes a row to its page.
/// </typeparam>
/// <typeparam name="TElement">
/// The type of the source rows.
/// </typeparam>
/// <remarks>
/// Rows arrive grouped by key. A row whose key differs from the previous row's key completes the
/// previous key's page; source exhaustion completes every page not yet complete, so a requested
/// key that never appears in the stream completes as an empty page. Pulling on any key's page
/// drives this pump; rows for other keys are buffered into their own pages. This type has no
/// cross-thread safety, the same stance as <see cref="StreamPageBuffer{TElement}"/>.
/// </remarks>
internal sealed class StreamBatchPump<TKey, TElement>
    where TKey : notnull
{
    private readonly IAsyncEnumerator<StreamBatchRow<TKey, TElement>> _source;
    private readonly Dictionary<TKey, KeyChannel> _keys;
    private IAsyncDisposable? _lifetime;
    private int _liveKeys;
    private bool _hasCurrentKey;
    private TKey _currentKey = default!;

    private StreamBatchPump(
        IAsyncEnumerator<StreamBatchRow<TKey, TElement>> source,
        IReadOnlyCollection<TKey> keys,
        IAsyncDisposable? lifetime)
    {
        _source = source;
        _lifetime = lifetime;
        _keys = new Dictionary<TKey, KeyChannel>(keys.Count);

        foreach (var key in keys)
        {
            if (!_keys.TryAdd(key, new KeyChannel()))
            {
                throw ThrowHelper.StreamBatchPump_DuplicateKey(key);
            }
        }

        _liveKeys = _keys.Count;
    }

    /// <summary>
    /// Creates a batch pump for the given requested keys, or, for an empty key set, disposes
    /// <paramref name="source"/> and <paramref name="lifetime"/> immediately and returns null.
    /// </summary>
    /// <param name="source">
    /// The shared, key-ordered source enumerator that produces rows for every requested key.
    /// </param>
    /// <param name="keys">
    /// The requested keys, which must not contain a duplicate. The returned pump releases the
    /// source and the lifetime once every one of these keys' pages has completed or been disposed.
    /// </param>
    /// <param name="lifetime">
    /// A resource owned by the pump, disposed once every key's page has completed or been
    /// disposed, or null if the pump owns nothing beyond the source.
    /// </param>
    /// <returns>
    /// Returns the batch pump, or null if <paramref name="keys"/> is empty and there is nothing
    /// to serve.
    /// </returns>
    public static async ValueTask<StreamBatchPump<TKey, TElement>?> CreateAsync(
        IAsyncEnumerator<StreamBatchRow<TKey, TElement>> source,
        IReadOnlyCollection<TKey> keys,
        IAsyncDisposable? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Count > 0)
        {
            return new StreamBatchPump<TKey, TElement>(source, keys, lifetime);
        }

        await source.DisposeAsync().ConfigureAwait(false);

        if (lifetime is not null)
        {
            await lifetime.DisposeAsync().ConfigureAwait(false);
        }

        return null;
    }

    /// <summary>
    /// Builds the page for the given requested key, wiring it to complete when the shared source
    /// moves past that key. Must be called exactly once for every key this batch pump was created
    /// with, before any page is primed.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of the page's items.
    /// </typeparam>
    /// <param name="key">
    /// One of the keys this batch pump was created with.
    /// </param>
    /// <param name="createPage">
    /// Builds the page from the per-key pump this batch pump creates for <paramref name="key"/>.
    /// </param>
    public StreamPage<TValue> CreatePage<TValue>(
        TKey key,
        Func<StreamPagePump<TElement>, StreamPage<TValue>> createPage)
    {
        ArgumentNullException.ThrowIfNull(createPage);

        if (!_keys.TryGetValue(key, out var channel))
        {
            throw ThrowHelper.StreamBatchPump_KeyNotRequested(key);
        }

        if (channel.Drain is not null)
        {
            throw ThrowHelper.StreamBatchPump_KeyAlreadyHasPage(key);
        }

        var pump = new StreamPagePump<TElement>(new KeyReader(this, key), pageCount: 1);
        var page = createPage(pump);
        channel.Drain = page.DrainAsync;
        return page;
    }

    // Returns the next row for the given key, reading from the shared source until one arrives,
    // the key's run completes, or the source is exhausted.
    private async ValueTask<StreamRow<TElement>?> ReadNextAsync(TKey key)
    {
        var channel = _keys[key];

        while (channel.Rows.Count == 0 && !channel.Completed)
        {
            await PumpOnceAsync().ConfigureAwait(false);
        }

        return channel.Rows.Count > 0 ? channel.Rows.Dequeue() : null;
    }

    // Advances the shared source by exactly one row, routing it to its key's channel. A key
    // change completes the previously active key's page; source exhaustion completes every page
    // not yet complete. Completing a channel here also drains its page, so the page's own
    // completion (and, once every key has completed or been disposed, the source and the
    // lifetime) happens without waiting for a consumer to pull the remaining buffered rows.
    private async ValueTask PumpOnceAsync()
    {
        if (!await _source.MoveNextAsync().ConfigureAwait(false))
        {
            foreach (var each in _keys.Values)
            {
                if (each.Completed)
                {
                    continue;
                }

                each.Completed = true;

                if (each.Drain is not null)
                {
                    await each.Drain(CancellationToken.None).ConfigureAwait(false);
                }
            }

            return;
        }

        var row = _source.Current;

        if (!_keys.TryGetValue(row.Key, out var channel))
        {
            throw ThrowHelper.StreamBatchPump_RowForUnrequestedKey(row.Key);
        }

        if (_hasCurrentKey && !EqualityComparer<TKey>.Default.Equals(_currentKey, row.Key))
        {
            var previous = _keys[_currentKey];
            previous.Completed = true;

            if (previous.Drain is not null)
            {
                await previous.Drain(CancellationToken.None).ConfigureAwait(false);
            }
        }

        _hasCurrentKey = true;
        _currentKey = row.Key;
        channel.Rows.Enqueue(new StreamRow<TElement>
        {
            Item = row.Item,
            TotalCount = row.TotalCount,
            HasMore = row.HasMore
        });
    }

    // Signals that one requested key's page has completed or been disposed. Once every key has
    // done so, disposes the source and then the lifetime, exactly once.
    private async ValueTask ReleaseAsync()
    {
        if (--_liveKeys > 0)
        {
            return;
        }

        await _source.DisposeAsync().ConfigureAwait(false);

        var lifetime = _lifetime;
        _lifetime = null;

        if (lifetime is not null)
        {
            await lifetime.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class KeyChannel
    {
        public Queue<StreamRow<TElement>> Rows { get; } = new();

        public bool Completed { get; set; }

        public Func<CancellationToken, ValueTask>? Drain { get; set; }
    }

    // Adapts one key's slice of the demultiplexed stream to the single-source shape a
    // StreamPagePump expects, and turns its disposal into this key's release signal.
    private sealed class KeyReader(StreamBatchPump<TKey, TElement> pump, TKey key)
        : IAsyncEnumerator<StreamRow<TElement>>
    {
        private StreamRow<TElement>? _current;

        public StreamRow<TElement> Current => _current!;

        public async ValueTask<bool> MoveNextAsync()
        {
            _current = await pump.ReadNextAsync(key).ConfigureAwait(false);
            return _current is not null;
        }

        public ValueTask DisposeAsync() => pump.ReleaseAsync();
    }
}
