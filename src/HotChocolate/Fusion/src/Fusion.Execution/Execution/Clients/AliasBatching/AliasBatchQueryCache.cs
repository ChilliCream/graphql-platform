using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// A bounded, thread-safe cache of the merged operation of batched requests, stored as the JSON
/// string value that is written to the request body. An entry is identified by everything the
/// merged operation is built from: the root field and lookup type of every item, the shared
/// variable definitions and the parts of the operation name.
/// </summary>
internal sealed class AliasBatchQueryCache
{
    /// <summary>
    /// The default maximum length in bytes of a merged operation that is cached.
    /// </summary>
    public const int DefaultMaxQueryLength = 16 * 1024;

    private const int DefaultBucketCount = 16;
    private const int BucketSize = 4;

    private readonly Entry?[] _entries;
    private readonly ulong[] _lastMissed;
    private readonly int[] _next;
    private readonly int _bucketMask;

    /// <summary>
    /// Initializes a new cache with the default size.
    /// </summary>
    public AliasBatchQueryCache()
        : this(DefaultBucketCount, DefaultMaxQueryLength)
    {
    }

    /// <summary>
    /// Initializes a new cache with at least <paramref name="bucketCount"/> buckets of four entries
    /// that only caches merged operations of at most <paramref name="maxQueryLength"/> bytes.
    /// </summary>
    public AliasBatchQueryCache(int bucketCount, int maxQueryLength)
    {
        var size = 1;

        while (size < bucketCount)
        {
            size <<= 1;
        }

        MaxQueryLength = maxQueryLength;
        _entries = new Entry?[size * BucketSize];
        _lastMissed = new ulong[size];
        _next = new int[size];
        _bucketMask = size - 1;
    }

    /// <summary>
    /// Gets the maximum length in bytes of a merged operation that is cached.
    /// </summary>
    public int MaxQueryLength { get; }

    /// <summary>
    /// Tries to get the merged operation that was built from the same inputs as <paramref name="key"/>.
    /// </summary>
    public bool TryGetQuery(in AliasBatchQueryKey key, out ReadOnlySpan<byte> query)
    {
        var start = GetBucket(key.CompositionHash) * BucketSize;

        for (var i = start; i < start + BucketSize; i++)
        {
            var entry = Volatile.Read(ref _entries[i]);

            if (entry?.Matches(in key) == true)
            {
                query = entry.Query;
                return true;
            }
        }

        query = default;
        return false;
    }

    /// <summary>
    /// Records a miss for <paramref name="key"/> and returns <c>true</c> when the miss recorded before
    /// it in the same bucket was for the same batch composition, which is when the merged operation
    /// should be stored with <see cref="Add"/>.
    /// </summary>
    public bool ShouldStore(in AliasBatchQueryKey key)
    {
        var bucket = GetBucket(key.CompositionHash);

        // Best effort: a composition that takes turns with other compositions of a full bucket
        // is not stored on every miss. Concurrent misses can store a composition earlier or later.
        if (Volatile.Read(ref _lastMissed[bucket]) == key.CompositionHash)
        {
            return true;
        }

        Volatile.Write(ref _lastMissed[bucket], key.CompositionHash);
        return false;
    }

    /// <summary>
    /// Stores the merged operation that was built from the inputs of <paramref name="key"/>.
    /// A full bucket replaces its entries in turn.
    /// </summary>
    public void Add(in AliasBatchQueryKey key, ReadOnlySpan<byte> query)
    {
        if (query.Length > MaxQueryLength)
        {
            return;
        }

        var bucket = GetBucket(key.CompositionHash);
        var start = bucket * BucketSize;
        var free = -1;

        for (var i = start; i < start + BucketSize; i++)
        {
            var current = Volatile.Read(ref _entries[i]);

            if (current is null)
            {
                if (free < 0)
                {
                    free = i;
                }
            }
            else if (current.Matches(in key))
            {
                // another thread stored the merged operation in the meantime.
                return;
            }
        }

        if (free < 0)
        {
            free = start + (int)((uint)Interlocked.Increment(ref _next[bucket]) % BucketSize);
        }

        Volatile.Write(ref _entries[free], new Entry(in key, query.ToArray()));
    }

    private int GetBucket(ulong compositionHash)
        => (int)(compositionHash ^ (compositionHash >> 32)) & _bucketMask;

    private sealed class Entry
    {
        private readonly ulong _compositionHash;
        private readonly string? _operationName;
        private readonly string _nameSafeShortHash;
        private readonly Utf8OperationDocument[] _itemDocuments;
        private readonly int[] _itemCursors;
        private readonly string[] _itemLookupTypeNames;
        private readonly Utf8OperationDocument[] _sharedDocuments;
        private readonly int[] _sharedCursors;

        public Entry(in AliasBatchQueryKey key, byte[] query)
        {
            var items = key.Items;
            var sharedVariables = key.SharedVariables;

            _compositionHash = key.CompositionHash;
            _operationName = key.OperationName;
            _nameSafeShortHash = key.NameSafeShortHash;
            _itemDocuments = new Utf8OperationDocument[items.Length];
            _itemCursors = new int[items.Length];
            _itemLookupTypeNames = new string[items.Length];
            _sharedDocuments = new Utf8OperationDocument[sharedVariables.Count];
            _sharedCursors = new int[sharedVariables.Count];
            Query = query;

            for (var i = 0; i < items.Length; i++)
            {
                var rootField = items[i].RootField;
                _itemDocuments[i] = rootField.Document;
                _itemCursors[i] = rootField.Cursor;
                _itemLookupTypeNames[i] = items[i].LookupTypeName;
            }

            for (var i = 0; i < sharedVariables.Count; i++)
            {
                var sharedVariable = sharedVariables[i];
                _sharedDocuments[i] = sharedVariable.Document;
                _sharedCursors[i] = sharedVariable.Cursor;
            }
        }

        public byte[] Query { get; }

        public bool Matches(in AliasBatchQueryKey key)
        {
            var items = key.Items;
            var sharedVariables = key.SharedVariables;

            if (_compositionHash != key.CompositionHash
                || items.Length != _itemDocuments.Length
                || sharedVariables.Count != _sharedDocuments.Length
                || !string.Equals(_operationName, key.OperationName, StringComparison.Ordinal)
                || !string.Equals(_nameSafeShortHash, key.NameSafeShortHash, StringComparison.Ordinal))
            {
                return false;
            }

            for (var i = 0; i < items.Length; i++)
            {
                var rootField = items[i].RootField;

                if (!ReferenceEquals(_itemDocuments[i], rootField.Document)
                    || _itemCursors[i] != rootField.Cursor
                    || !string.Equals(_itemLookupTypeNames[i], items[i].LookupTypeName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            for (var i = 0; i < sharedVariables.Count; i++)
            {
                var sharedVariable = sharedVariables[i];

                if (!ReferenceEquals(_sharedDocuments[i], sharedVariable.Document)
                    || _sharedCursors[i] != sharedVariable.Cursor)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
