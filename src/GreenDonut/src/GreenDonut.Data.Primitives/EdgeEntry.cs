namespace GreenDonut.Data;

/// <summary>
/// Bundles a node with the positional metadata needed for cursor generation.
/// </summary>
/// <typeparam name="T">
/// The type of the node.
/// </typeparam>
public readonly struct EdgeEntry<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EdgeEntry{T}"/> struct.
    /// </summary>
    /// <param name="node">
    /// The node (item from the page).
    /// </param>
    /// <param name="offset">
    /// The offset relative to the current cursor position.
    /// </param>
    /// <param name="pageIndex">
    /// The page index.
    /// </param>
    /// <param name="totalCount">
    /// The total count of items in the dataset.
    /// </param>
    public EdgeEntry(T node, int offset, int pageIndex, int totalCount)
    {
        Node = node;
        Offset = offset;
        PageIndex = pageIndex;
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the node (the item from the page).
    /// </summary>
    public T Node { get; }

    /// <summary>
    /// Gets the offset relative to the current cursor position.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the page index.
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Gets the total count of items in the dataset.
    /// </summary>
    public int TotalCount { get; }
}
