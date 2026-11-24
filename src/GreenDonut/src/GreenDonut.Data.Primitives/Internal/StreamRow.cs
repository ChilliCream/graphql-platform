namespace GreenDonut.Data.Internal;

/// <summary>
/// Carries one row from a streaming source, together with any page metadata that travels
/// alongside it. Query layers materialize rows directly into this type.
/// </summary>
/// <typeparam name="TElement">
/// The type of the row's item.
/// </typeparam>
internal sealed class StreamRow<TElement>
{
    /// <summary>
    /// Gets or sets the item carried by this row.
    /// </summary>
    public TElement Item { get; set; } = default!;

    /// <summary>
    /// Gets or sets the total count of items in the dataset, when it travels with this row.
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Gets or sets whether more rows exist beyond this one, when that flag travels with this row.
    /// </summary>
    public bool? HasMore { get; set; }
}
