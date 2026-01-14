using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

/// <summary>
/// Represents a collection of GraphQL enum values.
/// </summary>
public sealed class FusionEnumValueCollection
    : IReadOnlyList<FusionEnumValue>
    , IReadOnlyEnumValueCollection
{
    private readonly FusionEnumValue[] _values;
    private readonly int _length;
    private readonly FrozenDictionary<string, FusionEnumValue> _map;

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionEnumValueCollection"/> class.
    /// </summary>
    /// <param name="values">
    /// The array of enum values to store in the collection.
    /// </param>
    public FusionEnumValueCollection(FusionEnumValue[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _map = values.ToFrozenDictionary(t => t.Name);
        _values = values;
        _values.PartitionByAccessibility(out _length);
    }

    /// <summary>
    /// Gets the count of enum values in the collection.
    /// </summary>
    public int Count => _length;

    /// <summary>
    /// Gets the enum value with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value to retrieve.
    /// </param>
    /// <returns>
    /// The enum value with the specified name.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the enum value is not found.
    /// </exception>
    public FusionEnumValue this[string name]
    {
        get
        {
            var value = _map[name];

            if (value.IsInaccessible)
            {
                throw new KeyNotFoundException();
            }

            return value;
        }
    }

    IEnumValue IReadOnlyEnumValueCollection.this[string name] => this[name];

    /// <summary>
    /// Gets the enum value at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the enum value to retrieve.
    /// </param>
    /// <returns>
    /// The enum value at the specified index.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the index is outside the valid range.
    /// </exception>
    public FusionEnumValue this[int index]
    {
        get
        {
            if (index < 0 || index >= _length)
            {
                throw new IndexOutOfRangeException();
            }

            return _values[index];
        }
    }

    IEnumValue IReadOnlyList<IEnumValue>.this[int index] => this[index];

    /// <summary>
    /// Gets an enum value by name with optional support for
    /// inaccessible (internal) values.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value to retrieve.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible values can be retrieved; otherwise, only accessible values are returned.
    /// </param>
    /// <returns>
    /// The enum value with the specified name.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the enum value is not found.
    /// </exception>
    public FusionEnumValue GetValue(
        string name,
        bool allowInaccessibleFields)
    {
        var value = _map[name];

        if (!allowInaccessibleFields && value.IsInaccessible)
        {
            throw new KeyNotFoundException();
        }

        return value;
    }

    /// <summary>
    /// Gets an enum value at the specified index with optional support for
    /// inaccessible (internal) values.
    /// </summary>
    /// <param name="index">
    /// The zero-based index of the enum value to retrieve.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible values can be retrieved; otherwise, only accessible values are returned.
    /// </param>
    /// <returns>
    /// The enum value at the specified index.
    /// </returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the index is outside the valid range.
    /// </exception>
    public FusionEnumValue GetValueAt(
        int index,
        bool allowInaccessibleFields)
    {
        var maxIndex = allowInaccessibleFields ? _values.Length : _length;

        if (index < 0 || index >= maxIndex)
        {
            throw new IndexOutOfRangeException();
        }

        return _values[index];
    }

    /// <summary>
    /// Tries to get the <paramref name="value"/> for
    /// the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <param name="value">
    /// The GraphQL enum value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <paramref name="name"/> represents a value of this enum type;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(string name, [NotNullWhen(true)] out FusionEnumValue? value)
    {
        if (_map.TryGetValue(name, out value) && !value.IsInaccessible)
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Attempts to get an enum value by name with optional support for
    /// inaccessible (internal) values.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible values can be retrieved; otherwise, only accessible values are returned.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the enum value with the specified name if found;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the enum value was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue(
        string name,
        bool allowInaccessibleFields,
        [NotNullWhen(true)] out FusionEnumValue? value)
    {
        if (allowInaccessibleFields)
        {
            return _map.TryGetValue(name, out value);
        }

        if (_map.TryGetValue(name, out value) && !value.IsInaccessible)
        {
            return true;
        }

        value = null;
        return false;
    }

    bool IReadOnlyEnumValueCollection.TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
    {
        if (_map.TryGetValue(name, out var enumValue) && !enumValue.IsInaccessible)
        {
            value = enumValue;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Determines whether the collection contains an enum value with the specified name.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains an enum value with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name)
        => _map.TryGetValue(name, out var value) && !value.IsInaccessible;

    /// <summary>
    /// Determines whether the collection contains an enum value with the specified name,
    /// with optional support for inaccessible (internal) values.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, inaccessible values are included in the check; otherwise, only accessible values are checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains an enum value with the specified name; otherwise, <c>false</c>.
    /// </returns>
    public bool ContainsName(string name, bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _map.ContainsKey(name);
        }

        return _map.TryGetValue(name, out var value) && !value.IsInaccessible;
    }

    /// <summary>
    /// Returns an enumerator for the enum values in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueEnumerator"/> for the enum values.
    /// </returns>
    public ValueEnumerator AsEnumerable() => new(_values, _length);

    /// <summary>
    /// Returns an enumerator with optional support for inaccessible (internal) values.
    /// </summary>
    /// <param name="allowInaccessibleFields">
    /// If <c>true</c>, the enumerator includes inaccessible values; otherwise, only accessible values are included.
    /// </param>
    /// <returns>
    /// A <see cref="ValueEnumerator"/> for the enum values.
    /// </returns>
    public ValueEnumerator AsEnumerable(bool allowInaccessibleFields)
        => allowInaccessibleFields ? new(_values, _values.Length) : new(_values, _length);

    /// <summary>
    /// Returns an enumerator that iterates through the enum values in the collection.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueEnumerator"/> for the enum values.
    /// </returns>
    public ValueEnumerator GetEnumerator()
        => AsEnumerable();

    IEnumerator<FusionEnumValue> IEnumerable<FusionEnumValue>.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IEnumValue> IEnumerable<IEnumValue>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    /// Gets an empty enum value collection.
    /// </summary>
    public static FusionEnumValueCollection Empty { get; } = new([]);

    /// <summary>
    /// An enumerator for iterating through enum values.
    /// </summary>
    public struct ValueEnumerator : IEnumerator<FusionEnumValue>, IEnumerator<IEnumValue>
    {
        private readonly FusionEnumValue[] _values;
        private readonly int _length;
        private int _index;

        internal ValueEnumerator(FusionEnumValue[] values, int length)
        {
            _values = values;
            _length = length;
            _index = -1;
        }

        /// <summary>
        /// Gets the enum value at the current position of the enumerator.
        /// </summary>
        public readonly FusionEnumValue Current => _values[_index];

        readonly IEnumValue IEnumerator<IEnumValue>.Current => Current;

        readonly object IEnumerator.Current => Current;

        /// <summary>
        /// Advances the enumerator to the next enum value in the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next enum value;
        /// <c>false</c> if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            var index = _index + 1;

            if (index < _length)
            {
                _index = index;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first enum value in the collection.
        /// </summary>
        public void Reset()
        {
            _index = -1;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public readonly void Dispose()
        {
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="ValueEnumerator"/> for the collection.</returns>
        public readonly ValueEnumerator GetEnumerator() => this;
    }
}
