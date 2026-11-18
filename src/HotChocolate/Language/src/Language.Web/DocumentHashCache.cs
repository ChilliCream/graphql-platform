namespace HotChocolate.Language;

/// <summary>
/// A bounded, thread-safe cache that maps the JSON-encoded document string of a request
/// to the hash that a hash provider computed for it.
/// </summary>
internal sealed class DocumentHashCache
{
    /// <summary>
    /// The default maximum length in bytes of a JSON-encoded document string that is cached.
    /// </summary>
    public const int DefaultMaxDocumentLength = 16 * 1024;

    private const int DefaultBucketCount = 64;
    private const int BucketSize = 4;

    private readonly Entry?[] _entries;
    private readonly int[] _lastMissed;
    private readonly int[] _next;
    private readonly int _bucketMask;

    /// <summary>
    /// Initializes a new cache for the hashes of <paramref name="hashProvider"/>.
    /// </summary>
    public DocumentHashCache(IDocumentHashProvider hashProvider)
        : this(hashProvider, DefaultBucketCount, DefaultMaxDocumentLength)
    {
    }

    /// <summary>
    /// Initializes a new cache for the hashes of <paramref name="hashProvider"/> with at least
    /// <paramref name="bucketCount"/> buckets of four entries that only caches document strings
    /// of at most <paramref name="maxDocumentLength"/> bytes.
    /// </summary>
    public DocumentHashCache(IDocumentHashProvider hashProvider, int bucketCount, int maxDocumentLength)
    {
        var size = 1;

        while (size < bucketCount)
        {
            size <<= 1;
        }

        HashProvider = hashProvider;
        MaxDocumentLength = maxDocumentLength;
        _entries = new Entry?[size * BucketSize];
        _lastMissed = new int[size];
        _next = new int[size];
        _bucketMask = size - 1;
    }

    /// <summary>
    /// Gets the hash provider that computed the cached hashes.
    /// </summary>
    public IDocumentHashProvider HashProvider { get; }

    /// <summary>
    /// Gets the maximum length in bytes of a JSON-encoded document string that is cached.
    /// </summary>
    public int MaxDocumentLength { get; }

    /// <summary>
    /// Computes the lookup key of a JSON-encoded document string.
    /// </summary>
    public static int ComputeKey(ReadOnlySpan<byte> rawDocument)
    {
#if NET8_0_OR_GREATER
        var hashCode = new HashCode();
        hashCode.AddBytes(rawDocument);
        return hashCode.ToHashCode();
#else
        var hash = 2166136261u;

        foreach (var value in rawDocument)
        {
            hash = unchecked((hash ^ value) * 16777619u);
        }

        return unchecked((int)hash);
#endif
    }

    /// <summary>
    /// Tries to get the hash of the document string that is byte for byte equal to
    /// <paramref name="rawDocument"/>.
    /// </summary>
    public bool TryGetHash(ReadOnlySpan<byte> rawDocument, int key, out OperationDocumentHash hash)
    {
        var start = (key & _bucketMask) * BucketSize;

        for (var i = start; i < start + BucketSize; i++)
        {
            var entry = Volatile.Read(ref _entries[i]);

            if (entry is not null
                && entry.Key == key
                && rawDocument.SequenceEqual(entry.RawDocument))
            {
                hash = entry.Hash;
                return true;
            }
        }

        hash = default;
        return false;
    }

    /// <summary>
    /// Records a miss for <paramref name="rawDocument"/> and stores its hash when the previous miss
    /// in its bucket was for the same key. A full bucket replaces its entries in turn.
    /// </summary>
    public void Add(ReadOnlySpan<byte> rawDocument, int key, OperationDocumentHash hash)
    {
        if (rawDocument.Length > MaxDocumentLength)
        {
            return;
        }

        var bucket = key & _bucketMask;

        // a document that takes turns with other documents of a full bucket is not stored on every miss.
        if (Volatile.Read(ref _lastMissed[bucket]) != key)
        {
            Volatile.Write(ref _lastMissed[bucket], key);
            return;
        }

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
            else if (current.Key == key && rawDocument.SequenceEqual(current.RawDocument))
            {
                // another thread stored the document in the meantime.
                return;
            }
        }

        if (free < 0)
        {
            free = start + (int)((uint)Interlocked.Increment(ref _next[bucket]) % BucketSize);
        }

        Volatile.Write(ref _entries[free], new Entry(key, rawDocument.ToArray(), hash));
    }

    private sealed class Entry(int key, byte[] rawDocument, OperationDocumentHash hash)
    {
        public int Key { get; } = key;

        public byte[] RawDocument { get; } = rawDocument;

        public OperationDocumentHash Hash { get; } = hash;
    }
}
