namespace GreenDonut.Data;

/// <summary>
/// Represents an entry in a page, bundling the item with its position.
/// </summary>
/// <typeparam name="T">
/// The type of the item.
/// </typeparam>
public readonly struct PageEntry<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageEntry{T}"/> struct.
    /// </summary>
    /// <param name="item">
    /// The item from the page.
    /// </param>
    /// <param name="index">
    /// The zero-based index of the item within the page.
    /// </param>
    public PageEntry(T item, int index)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);

        Item = item;
        Index = index;
    }

    /// <summary>
    /// Gets the item from the page.
    /// </summary>
    public T Item { get; }

    /// <summary>
    /// Gets the zero-based index of the item within the page.
    /// </summary>
    public int Index { get; }
}
