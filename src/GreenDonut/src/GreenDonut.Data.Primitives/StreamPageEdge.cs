namespace GreenDonut.Data;

/// <summary>
/// Represents an item and its cursor in a streamed page.
/// </summary>
/// <typeparam name="T">
/// The type of the item.
/// </typeparam>
public readonly struct StreamPageEdge<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamPageEdge{T}"/> struct.
    /// </summary>
    /// <param name="node">
    /// The item at the end of the edge.
    /// </param>
    /// <param name="cursor">
    /// The cursor for the item.
    /// </param>
    public StreamPageEdge(T node, string cursor)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(cursor);

        Node = node;
        Cursor = cursor;
    }

    /// <summary>
    /// Gets the item at the end of the edge.
    /// </summary>
    public T Node { get; }

    /// <summary>
    /// Gets the cursor for the item.
    /// </summary>
    public string Cursor { get; }
}
