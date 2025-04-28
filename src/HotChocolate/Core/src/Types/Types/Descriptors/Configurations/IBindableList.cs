namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// A list that also exposes the binding behavior.
/// </summary>
/// <typeparam name="T">
/// The element type.
/// </typeparam>
public interface IBindableList<T> : IList<T>, IReadOnlyList<T>
{
    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    new int Count { get; }

    /// <summary>Gets or sets the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the
    /// <see cref="T:System.Collections.Generic.IList`1" />.
    /// </exception>
    /// <exception cref="T:System.NotSupportedException">
    /// The property is set and the <see cref="T:System.Collections.Generic.IList`1" />
    /// is read-only.
    /// </exception>
    /// <returns>The element at the specified index.</returns>
    new T this[int index] { get; set; }

    /// <summary>
    /// Defines how items of the list can be bound.
    /// </summary>
    BindingBehavior BindingBehavior { get; set; }

    /// <summary>
    /// Adds new items to this list.
    /// </summary>
    /// <param name="items">
    /// The items that shall be added.
    /// </param>
    void AddRange(IEnumerable<T> items);
}
