namespace HotChocolate.Execution;

/// <summary>
/// Represents one item of a streamed list together with its position in the full list.
/// </summary>
public readonly struct OperationResultItem
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResultItem"/>.
    /// </summary>
    /// <param name="index">
    /// The index of the item in the full list.
    /// </param>
    /// <param name="element">
    /// The element that represents the item.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="element"/> is <c>null</c>.
    /// </exception>
    public OperationResultItem(int index, object element)
    {
        ArgumentNullException.ThrowIfNull(element);

        Index = index;
        Element = element;
    }

    /// <summary>
    /// Gets the index of the item in the full list.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the element that represents the item.
    /// </summary>
    public object Element { get; }
}
