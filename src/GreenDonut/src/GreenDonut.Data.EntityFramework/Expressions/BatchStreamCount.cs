namespace GreenDonut.Data.Expressions;

/// <summary>
/// A per-key count result: the full row count for a key, and optionally the number of its
/// rows matching an additional predicate.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key.
/// </typeparam>
internal sealed class BatchStreamCount<TKey>
{
    public TKey Key { get; set; } = default!;

    public int Count { get; set; }

    public int PredicateCount { get; set; }
}
