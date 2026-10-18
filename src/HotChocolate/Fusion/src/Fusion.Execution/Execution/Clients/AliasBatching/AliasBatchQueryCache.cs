using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients.AliasBatching;

/// <summary>
/// Holds the merged operations of the batched requests a plan node sends, keyed by the client
/// operation and by the root selection and lookup type of every item, in item order. It holds a
/// bounded number of compositions and admits a composition when it repeats.
/// </summary>
internal sealed class AliasBatchQueryCache
{
    private const int Capacity = 4;

    /// <summary>
    /// The largest number of items a cached composition holds.
    /// </summary>
    public const int MaxItemCount = 64;

    private readonly Entry?[] _entries = new Entry?[Capacity];
    private int _nextSlot;
    private ulong _candidate;

    /// <summary>
    /// Gets the merged operation of the batch composition that the items describe.
    /// </summary>
    /// <param name="operationName">
    /// The name of the client operation, or <see langword="null"/> when it is anonymous.
    /// </param>
    /// <param name="operationShortHash">The short hash of the client operation.</param>
    /// <param name="items">The items of the batch.</param>
    /// <param name="query">
    /// The merged operation as a JSON string value, or empty when the composition is not cached.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the composition is cached; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetQuery(
        string? operationName,
        string operationShortHash,
        ReadOnlySpan<AliasBatchItem> items,
        out ReadOnlyMemory<byte> query)
    {
        if (items.Length <= MaxItemCount)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                var entry = Volatile.Read(ref _entries[i]);

                if (entry?.Matches(operationName, operationShortHash, items) == true)
                {
                    query = entry.Query;
                    return true;
                }
            }
        }

        query = default;
        return false;
    }

    /// <summary>
    /// Records a composition that was not cached and returns whether it is to be cached.
    /// </summary>
    /// <param name="compositionHash">The hash of the batch composition.</param>
    /// <param name="itemCount">The number of items of the batch.</param>
    /// <returns>
    /// <see langword="true"/> when the composition repeats the one recorded last and holds at
    /// most <see cref="MaxItemCount"/> items; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ShouldAdd(ulong compositionHash, int itemCount)
        => itemCount <= MaxItemCount
            && Interlocked.Exchange(ref _candidate, compositionHash) == compositionHash;

    /// <summary>
    /// Caches the merged operation of the batch composition that the items describe.
    /// </summary>
    /// <param name="operationName">
    /// The name of the client operation, or <see langword="null"/> when it is anonymous.
    /// </param>
    /// <param name="operationShortHash">The short hash of the client operation.</param>
    /// <param name="items">The items of the batch.</param>
    /// <param name="query">The merged operation as a JSON string value.</param>
    public void Add(
        string? operationName,
        string operationShortHash,
        ReadOnlySpan<AliasBatchItem> items,
        byte[] query)
    {
        var composition = new CompositionItem[items.Length];

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            composition[i] = new CompositionItem(item.RootField.Document, item.LookupTypeName);
        }

        var slot = (int)((uint)Interlocked.Increment(ref _nextSlot) % Capacity);
        Volatile.Write(
            ref _entries[slot],
            new Entry(operationName, operationShortHash, composition, query));
    }

    private sealed class Entry(
        string? operationName,
        string operationShortHash,
        CompositionItem[] composition,
        byte[] query)
    {
        public byte[] Query { get; } = query;

        public bool Matches(
            string? otherOperationName,
            string otherOperationShortHash,
            ReadOnlySpan<AliasBatchItem> items)
        {
            if (composition.Length != items.Length
                || !string.Equals(operationShortHash, otherOperationShortHash, StringComparison.Ordinal)
                || !string.Equals(operationName, otherOperationName, StringComparison.Ordinal))
            {
                return false;
            }

            for (var i = 0; i < composition.Length; i++)
            {
                var item = items[i];
                var expected = composition[i];

                // A batched request's document holds exactly one root selection.
                if (!ReferenceEquals(expected.Document, item.RootField.Document)
                    || !string.Equals(expected.LookupTypeName, item.LookupTypeName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private readonly struct CompositionItem(Utf8OperationDocument document, string lookupTypeName)
    {
        public Utf8OperationDocument Document { get; } = document;

        public string LookupTypeName { get; } = lookupTypeName;
    }
}
