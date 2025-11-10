using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionEnumValueCollection
    : IReadOnlyList<FusionEnumValue>
    , IReadOnlyEnumValueCollection
{
    private readonly FusionEnumValue[] _values;
    private readonly int _length;
    private readonly FrozenDictionary<string, FusionEnumValue> _map;

    public FusionEnumValueCollection(FusionEnumValue[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        Partitioner.PartitionByAccessibility(values, out _length);

        _map = values.ToFrozenDictionary(t => t.Name);
        _values = values;
    }

    public int Count => _length;

    /// <summary>
    /// Gets the enum value with the specified name.
    /// </summary>
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

    public bool ContainsName(string name, bool allowInaccessibleFields)
    {
        if (allowInaccessibleFields)
        {
            return _map.ContainsKey(name);
        }

        return _map.TryGetValue(name, out var value) && !value.IsInaccessible;
    }

    public ValueEnumerator AsEnumerable() => new(_values, _length);

    public ValueEnumerator AsEnumerable(bool allowInaccessibleFields)
        => allowInaccessibleFields ? new(_values, _values.Length) : new(_values, _length);

    public ValueEnumerator GetEnumerator()
        => AsEnumerable();

    IEnumerator<FusionEnumValue> IEnumerable<FusionEnumValue>.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IEnumValue> IEnumerable<IEnumValue>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static FusionEnumValueCollection Empty { get; } = new([]);

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

        public readonly FusionEnumValue Current => _values[_index];

        readonly IEnumValue IEnumerator<IEnumValue>.Current => Current;

        readonly object IEnumerator.Current => Current;

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

        public void Reset()
        {
            _index = -1;
        }

        public readonly void Dispose()
        {
        }

        public readonly ValueEnumerator GetEnumerator() => this;
    }

    private static class Partitioner
    {
        public static void PartitionByAccessibility(FusionEnumValue[] array, out int length)
        {
            var writeIndex = 0;

            for (var i = 0; i < array.Length; i++)
            {
                if (!array[i].IsInaccessible)
                {
                    if (i != writeIndex)
                    {
                        (array[writeIndex], array[i]) = (array[i], array[writeIndex]);
                    }
                    writeIndex++;
                }
            }

            length = writeIndex;
        }
    }
}
