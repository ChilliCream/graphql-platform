using System.Collections;

namespace HotChocolate.Text.Yaml;

/// <summary>
/// A value in a <see cref="YamlDocument"/>.
/// </summary>
public readonly struct YamlElement
{
    private readonly YamlDocument? _parent;
    private readonly int _index;

    internal YamlElement(YamlDocument parent, int index)
    {
        _parent = parent;
        _index = index;
    }

    /// <summary>
    /// Gets the kind of the value.
    /// </summary>
    public YamlValueKind ValueKind
        => _parent is null ? YamlValueKind.Undefined : _parent.GetRow(_index).Kind;

    /// <summary>
    /// Gets the 1-based line on which the value starts.
    /// </summary>
    public int Line => Parent.GetRow(_index).Line;

    /// <summary>
    /// Gets the 1-based column at which the value starts.
    /// </summary>
    public int Column => Parent.GetRow(_index).Column;

    private YamlDocument Parent
        => _parent ?? throw ThrowHelper.UndefinedElement();

    /// <summary>
    /// Gets a property by its case-sensitive name.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not an object.</exception>
    /// <exception cref="KeyNotFoundException">The object has no property with this name.</exception>
    public YamlElement GetProperty(string propertyName)
    {
        if (TryGetProperty(propertyName, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException(propertyName);
    }

    /// <summary>
    /// Looks up a property by its case-sensitive name.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if found. Otherwise, <paramref name="value"/> is undefined.
    /// </returns>
    /// <exception cref="InvalidOperationException">The value is not an object.</exception>
    public bool TryGetProperty(string propertyName, out YamlElement value)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        foreach (var property in EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.Ordinal))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Enumerates the properties of an object.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not an object.</exception>
    public ObjectEnumerator EnumerateObject()
    {
        EnsureKind(YamlValueKind.Object);
        return new ObjectEnumerator(_parent!, _index);
    }

    /// <summary>
    /// Gets the number of items in an array.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not an array.</exception>
    public int GetArrayLength()
    {
        EnsureKind(YamlValueKind.Array);
        return _parent!.GetRow(_index).ChildCount;
    }

    /// <summary>
    /// Enumerates the items of an array.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not an array.</exception>
    public ArrayEnumerator EnumerateArray()
    {
        EnsureKind(YamlValueKind.Array);
        return new ArrayEnumerator(_parent!, _index);
    }

    /// <summary>
    /// Gets the value of a string, or <c>null</c> for a null value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is neither a string nor null.</exception>
    public string? GetString()
    {
        var kind = ValueKind;

        if (kind == YamlValueKind.Null)
        {
            return null;
        }

        EnsureKind(YamlValueKind.String);
        return _parent!.GetString(_index);
    }

    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not a boolean.</exception>
    public bool GetBoolean()
        => ValueKind switch
        {
            YamlValueKind.True => true,
            YamlValueKind.False => false,
            var kind => throw ThrowHelper.WrongKind(YamlValueKind.True, kind)
        };

    /// <summary>
    /// Gets the number as an <see cref="int"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not a number.</exception>
    /// <exception cref="FormatException">The number does not fit an <see cref="int"/>.</exception>
    public int GetInt32()
    {
        EnsureKind(YamlValueKind.Number);
        return _parent!.TryGetInt32(_index, out var value) ? value : throw new FormatException();
    }

    /// <summary>
    /// Gets the number as a <see cref="long"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not a number.</exception>
    /// <exception cref="FormatException">The number does not fit a <see cref="long"/>.</exception>
    public long GetInt64()
    {
        EnsureKind(YamlValueKind.Number);
        return _parent!.TryGetInt64(_index, out var value) ? value : throw new FormatException();
    }

    /// <summary>
    /// Gets the number as a <see cref="double"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is not a number.</exception>
    public double GetDouble()
    {
        EnsureKind(YamlValueKind.Number);
        return _parent!.TryGetDouble(_index, out var value) ? value : throw new FormatException();
    }

    /// <summary>
    /// Gets the text of a scalar of any kind, or an empty string for a null value or a collection.
    /// </summary>
    public override string ToString()
        => ValueKind switch
        {
            YamlValueKind.String or YamlValueKind.Number or YamlValueKind.True or YamlValueKind.False
                => _parent!.GetString(_index),
            _ => string.Empty
        };

    private void EnsureKind(YamlValueKind expected)
    {
        var kind = ValueKind;

        if (kind != expected)
        {
            throw ThrowHelper.WrongKind(expected, kind);
        }
    }

    /// <summary>
    /// An enumerator over the items of an array.
    /// </summary>
    public struct ArrayEnumerator : IEnumerable<YamlElement>, IEnumerator<YamlElement>
    {
        private readonly YamlDocument _parent;
        private readonly int _start;
        private readonly int _end;
        private int _current;

        internal ArrayEnumerator(YamlDocument parent, int index)
        {
            _parent = parent;
            _start = index;
            _end = index + parent.GetRow(index).NumberOfRows;
            _current = -1;
        }

        /// <inheritdoc />
        public readonly YamlElement Current => new(_parent, _current);

        readonly object IEnumerator.Current => Current;

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        public readonly ArrayEnumerator GetEnumerator() => this;

        readonly IEnumerator<YamlElement> IEnumerable<YamlElement>.GetEnumerator() => this;

        readonly IEnumerator IEnumerable.GetEnumerator() => this;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_current >= _end)
            {
                return false;
            }

            _current = _current < 0 ? _start + 1 : _current + _parent.GetRow(_current).NumberOfRows;
            return _current < _end;
        }

        /// <inheritdoc />
        public void Reset() => _current = -1;

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }

    /// <summary>
    /// An enumerator over the properties of an object.
    /// </summary>
    public struct ObjectEnumerator : IEnumerable<YamlProperty>, IEnumerator<YamlProperty>
    {
        private readonly YamlDocument _parent;
        private readonly int _start;
        private readonly int _end;
        private int _current;

        internal ObjectEnumerator(YamlDocument parent, int index)
        {
            _parent = parent;
            _start = index;
            _end = index + parent.GetRow(index).NumberOfRows;
            _current = -1;
        }

        /// <inheritdoc />
        public readonly YamlProperty Current => new(_parent, _current);

        readonly object IEnumerator.Current => Current;

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        public readonly ObjectEnumerator GetEnumerator() => this;

        readonly IEnumerator<YamlProperty> IEnumerable<YamlProperty>.GetEnumerator() => this;

        readonly IEnumerator IEnumerable.GetEnumerator() => this;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_current >= _end)
            {
                return false;
            }

            _current = _current < 0 ? _start + 1 : _current + 1 + _parent.GetRow(_current + 1).NumberOfRows;
            return _current < _end;
        }

        /// <inheritdoc />
        public void Reset() => _current = -1;

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
