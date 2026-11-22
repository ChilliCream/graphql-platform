using System.Diagnostics;

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

    private const int DefaultBucketCount = 32;
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
        Debug.Assert(bucketCount is > 0 and <= 1 << 16, "The bucket count must be between 1 and 65536.");

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
    /// Creates a cache for the hashes of <paramref name="hashProvider"/>. Returns <c>null</c> when the
    /// provider is not one of the built-in providers, which always compute the same hash for a document.
    /// </summary>
    public static DocumentHashCache? TryCreate(IDocumentHashProvider hashProvider)
        => hashProvider is MD5DocumentHashProvider or Sha1DocumentHashProvider or Sha256DocumentHashProvider
            ? new DocumentHashCache(hashProvider)
            : null;

    /// <summary>
    /// Tries to get the hash of the document string that is byte for byte equal to
    /// <paramref name="rawDocument"/> and was hashed with the same <paramref name="isEscaped"/> mode.
    /// </summary>
    /// <param name="rawDocument">The JSON-encoded document string without its quotes.</param>
    /// <param name="isEscaped">
    /// <c>true</c> when the document string is unescaped before it is hashed.
    /// </param>
    /// <param name="hash">The hash of the document.</param>
    public bool TryGetHash(ReadOnlySpan<byte> rawDocument, bool isEscaped, out OperationDocumentHash hash)
    {
        var key = ComputeKey(rawDocument, isEscaped);
        var start = (key & _bucketMask) * BucketSize;

        for (var i = start; i < start + BucketSize; i++)
        {
            var entry = Volatile.Read(ref _entries[i]);

            if (entry?.Matches(rawDocument, isEscaped, key) == true)
            {
                hash = entry.Hash;
                return true;
            }
        }

        hash = default;
        return false;
    }

    /// <summary>
    /// Records a miss for <paramref name="rawDocument"/> and stores its hash when the miss recorded
    /// before it in the same bucket was for the same document. A full bucket replaces its entries
    /// in turn.
    /// </summary>
    /// <param name="rawDocument">The JSON-encoded document string without its quotes.</param>
    /// <param name="isEscaped">
    /// <c>true</c> when the document string is unescaped before it is hashed.
    /// </param>
    /// <param name="hash">The hash of the document.</param>
    public void Add(ReadOnlySpan<byte> rawDocument, bool isEscaped, OperationDocumentHash hash)
    {
        if (rawDocument.Length > MaxDocumentLength)
        {
            return;
        }

        var key = ComputeKey(rawDocument, isEscaped);
        var bucket = key & _bucketMask;

        // Best effort: a document that takes turns with other documents of a full bucket
        // is not stored on every miss. Concurrent misses can store a document earlier or later.
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
            else if (current.Matches(rawDocument, isEscaped, key))
            {
                // another thread stored the document in the meantime.
                return;
            }
        }

        if (free < 0)
        {
            free = start + (int)((uint)Interlocked.Increment(ref _next[bucket]) % BucketSize);
        }

        Volatile.Write(ref _entries[free], new Entry(key, isEscaped, rawDocument.ToArray(), hash));
    }

    private static int ComputeKey(ReadOnlySpan<byte> rawDocument, bool isEscaped)
    {
#if NET8_0_OR_GREATER
        var hashCode = new HashCode();
        hashCode.AddBytes(rawDocument);
        hashCode.Add(isEscaped);
        return hashCode.ToHashCode();
#else
        var hash = isEscaped ? 2166136261u : 2166136260u;

        foreach (var value in rawDocument)
        {
            hash = unchecked((hash ^ value) * 16777619u);
        }

        return unchecked((int)hash);
#endif
    }

    private sealed class Entry(int key, bool isEscaped, byte[] rawDocument, OperationDocumentHash hash)
    {
        public OperationDocumentHash Hash { get; } = hash;

        public bool Matches(ReadOnlySpan<byte> otherRawDocument, bool otherIsEscaped, int otherKey)
            => key == otherKey
                && isEscaped == otherIsEscaped
                && otherRawDocument.SequenceEqual(rawDocument);
    }
}
