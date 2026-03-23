using System.Collections;
using System.Collections.Immutable;

namespace Mocha.Analyzers.Utils;

/// <summary>
/// Represents an immutable list that implements sequence-based equality comparison.
/// </summary>
/// <typeparam name="T">The element type, which must implement <see cref="IEquatable{T}"/>.</typeparam>
/// <remarks>
/// Two instances are considered equal if they contain the same elements in the same order.
/// This is suitable for use in incremental generator pipelines where value equality
/// is required for caching.
/// </remarks>
public sealed class ImmutableEquatableArray<T> : IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// Gets an empty <see cref="ImmutableEquatableArray{T}"/>.
    /// </summary>
    public static ImmutableEquatableArray<T> Empty { get; } = new(ImmutableArray.Create<T>());

    private readonly ImmutableArray<T> _values;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index] => _values[index];

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _values.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableEquatableArray{T}"/> class from the specified sequence.
    /// </summary>
    /// <param name="values">The values to include in the array.</param>
    public ImmutableEquatableArray(IEnumerable<T> values) => _values = values.ToImmutableArray();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableEquatableArray{T}"/> class from the specified immutable array.
    /// </summary>
    /// <param name="values">The immutable array to wrap.</param>
    public ImmutableEquatableArray(ImmutableArray<T> values) => _values = values;

    /// <summary>
    /// Creates a new array with the specified value appended.
    /// </summary>
    /// <param name="value">The value to add.</param>
    /// <returns>A new <see cref="ImmutableEquatableArray{T}"/> with the value appended.</returns>
    public ImmutableEquatableArray<T> Add(T value) => new(_values.Add(value));

    /// <summary>
    /// Gets a value indicating whether this array contains no elements.
    /// </summary>
    public bool IsEmpty => _values.IsEmpty;

    /// <summary>
    /// Creates a new array with the specified values appended.
    /// </summary>
    /// <param name="values">The values to add.</param>
    /// <returns>A new <see cref="ImmutableEquatableArray{T}"/> with the values appended.</returns>
    public ImmutableEquatableArray<T> AddRange(IEnumerable<T> values) => new(_values.AddRange(values));

    /// <summary>
    /// Determines whether this array is equal to another by comparing elements in sequence.
    /// </summary>
    /// <param name="other">The other array to compare with.</param>
    /// <returns><see langword="true"/> if both arrays contain the same elements in the same order; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ImmutableEquatableArray<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _values.SequenceEqual(other._values);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not ImmutableEquatableArray<T> other)
        {
            return false;
        }

        return Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (var item in _values)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();
}
