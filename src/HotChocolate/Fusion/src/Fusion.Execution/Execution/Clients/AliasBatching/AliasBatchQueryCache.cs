using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Caching.Memory;
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

    private const int DefaultCapacity = 64;
    private const int MissSlotCount = 16;
#if NET9_0_OR_GREATER
    private const int KeyLength = 16;
#endif

    private readonly Cache<Entry> _cache;
    private readonly ulong[] _lastMissed = new ulong[MissSlotCount];

    /// <summary>
    /// Initializes a new cache with the default size.
    /// </summary>
    public AliasBatchQueryCache()
        : this(DefaultCapacity, DefaultMaxQueryLength)
    {
    }

    /// <summary>
    /// Initializes a new cache that holds up to <paramref name="capacity"/> merged operations
    /// of at most <paramref name="maxQueryLength"/> bytes each.
    /// </summary>
    public AliasBatchQueryCache(int capacity, int maxQueryLength)
    {
        MaxQueryLength = maxQueryLength;
        _cache = new Cache<Entry>(capacity);
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
        // The cache is keyed on the hash of the composition, so the entry is compared with the inputs.
        if (TryGetEntry(key.CompositionHash, out var entry) && entry.Matches(in key))
        {
            query = entry.Query;
            return true;
        }

        query = default;
        return false;
    }

    /// <summary>
    /// Records a miss for <paramref name="key"/> and returns <c>true</c> when the miss recorded before
    /// it in the same slot was for the same batch composition, which is when the merged operation
    /// should be stored with <see cref="Add"/>.
    /// </summary>
    public bool ShouldStore(in AliasBatchQueryKey key)
    {
        var compositionHash = key.CompositionHash;
        var slot = (int)(compositionHash ^ (compositionHash >> 32)) & (MissSlotCount - 1);

        // Best effort: a composition that takes turns with other compositions of its slot
        // is not stored on every miss. Concurrent misses can store a composition earlier or later.
        if (Volatile.Read(ref _lastMissed[slot]) == compositionHash)
        {
            return true;
        }

        Volatile.Write(ref _lastMissed[slot], compositionHash);
        return false;
    }

    /// <summary>
    /// Stores the merged operation that was built from the inputs of <paramref name="key"/>.
    /// A composition whose hash is already stored is not stored again.
    /// </summary>
    public void Add(in AliasBatchQueryKey key, ReadOnlySpan<byte> query)
    {
        if (query.Length > MaxQueryLength || TryGetEntry(key.CompositionHash, out _))
        {
            return;
        }

        var entry = new Entry(in key, query.ToArray());

#if NET9_0_OR_GREATER
        Span<char> name = stackalloc char[KeyLength];
        FormatKey(key.CompositionHash, name);
        _cache.TryAdd(name, entry);
#else
        _cache.TryAdd(FormatKey(key.CompositionHash), entry);
#endif
    }

    private bool TryGetEntry(ulong compositionHash, [NotNullWhen(true)] out Entry? entry)
    {
#if NET9_0_OR_GREATER
        Span<char> name = stackalloc char[KeyLength];
        FormatKey(compositionHash, name);
        return _cache.TryGet(name, out entry);
#else
        return _cache.TryGet(FormatKey(compositionHash), out entry);
#endif
    }

#if NET9_0_OR_GREATER
    private static void FormatKey(ulong compositionHash, Span<char> destination)
        => compositionHash.TryFormat(destination, out _, "x16", CultureInfo.InvariantCulture);
#else
    private static string FormatKey(ulong compositionHash)
        => compositionHash.ToString("x16", CultureInfo.InvariantCulture);
#endif

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
