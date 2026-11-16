namespace GreenDonut.Data.Internal;

/// <summary>
/// Carries one row from a batch streaming source, together with the key that routes it to its
/// page and any page metadata that travels alongside it. Query layers materialize rows directly
/// into this type.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key that routes this row to its page.
/// </typeparam>
/// <typeparam name="TElement">
/// The type of the row's item.
/// </typeparam>
internal sealed class StreamBatchRow<TKey, TElement>
    where TKey : notnull
{
    /// <summary>
    /// Gets or sets the key that routes this row to its page.
    /// </summary>
    public TKey Key { get; set; } = default!;

    /// <summary>
    /// Gets or sets the item carried by this row.
    /// </summary>
    public TElement Item { get; set; } = default!;

    /// <summary>
    /// Gets or sets the total count of items for this row's key, when it travels with this row.
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether more rows exist beyond this one for this row's key, when that flag
    /// travels with this row.
    /// </summary>
    public bool? HasMore { get; set; }
}
