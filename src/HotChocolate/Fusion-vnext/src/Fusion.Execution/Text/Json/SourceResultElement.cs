using System.Diagnostics;
using System.Text.Json;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct SourceResultElement
{
    internal readonly SourceResultDocument _parent;
    internal readonly int _index;

    //     internal int Size;
    //    internal bool HasComplexChildren;

    internal SourceResultElement(SourceResultDocument parent, int index)
    {
        // parent is usually not null, but the Current property
        // on the enumerators (when initialized as `default`) can
        // get here with a null.
        Debug.Assert(index >= 0);

        _parent = parent;
        _index = index;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal JsonTokenType TokenType => _parent?.GetElementTokenType(_index) ?? JsonTokenType.None;

    /// <summary>
    /// The <see cref="JsonValueKind"/> that the value is.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public JsonValueKind ValueKind => TokenType.ToValueKind();

    /// <summary>
    /// Get the value at a specified index when the current value is a
    /// <see cref="JsonValueKind.Array"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Array"/>.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="index"/> is not in the range [0, <see cref="GetArrayLength"/>()).
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement this[int index]
    {
        get
        {
            CheckValidInstance();

            return _parent.GetArrayIndexElement(_index, index);
        }
    }

    /// <summary>
    ///   Get the number of values contained within the current array value.
    /// </summary>
    /// <returns>The number of values contained within the current array value.</returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Array"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public int GetArrayLength()
    {
        CheckValidInstance();

        return _parent.GetArrayLength(_index);
    }

    /// <summary>
    ///   Get the number of properties contained within the current object value.
    /// </summary>
    /// <returns>The number of properties contained within the current object value.</returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public int GetPropertyCount()
    {
        CheckValidInstance();

        return _parent.GetPropertyCount(_index);
    }

    /// <summary>
    /// Gets a <see cref="JsonElement"/> representing the value of a required property identified
    /// by <paramref name="propertyName"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Property name matching is performed as an ordinal, case-sensitive, comparison.
    /// </para>
    /// <para>
    /// If a property is defined multiple times for the same object, the last such definition is
    /// what is matched.
    /// </para>
    /// </remarks>
    /// <param name="propertyName">Name of the property whose value to return.</param>
    /// <returns>
    /// A <see cref="JsonElement"/> representing the value of the requested property.
    /// </returns>
    /// <seealso cref="EnumerateObject"/>
    /// <exception cref="InvalidOperationException">
    /// This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No property was found with the requested name.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="propertyName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement GetProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (TryGetProperty(propertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    /// <summary>
    ///   Gets a <see cref="JsonElement"/> representing the value of a required property identified
    ///   by <paramref name="propertyName"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
    ///   </para>
    ///
    ///   <para>
    ///     If a property is defined multiple times for the same object, the last such definition is
    ///     what is matched.
    ///   </para>
    /// </remarks>
    /// <param name="propertyName">Name of the property whose value to return.</param>
    /// <returns>
    ///   A <see cref="JsonElement"/> representing the value of the requested property.
    /// </returns>
    /// <seealso cref="EnumerateObject"/>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    ///   No property was found with the requested name.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement GetProperty(ReadOnlySpan<char> propertyName)
    {
        if (TryGetProperty(propertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    /// <summary>
    ///   Gets a <see cref="JsonElement"/> representing the value of a required property identified
    ///   by <paramref name="utf8PropertyName"/>.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
    ///   </para>
    ///
    ///   <para>
    ///     If a property is defined multiple times for the same object, the last such definition is
    ///     what is matched.
    ///   </para>
    /// </remarks>
    /// <param name="utf8PropertyName">
    ///   The UTF-8 (with no Byte-Order-Mark (BOM)) representation of the name of the property to return.
    /// </param>
    /// <returns>
    ///   A <see cref="JsonElement"/> representing the value of the requested property.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    ///   No property was found with the requested name.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    /// <seealso cref="EnumerateObject"/>
    public SourceResultElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
    {
        if (TryGetProperty(utf8PropertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    /// <summary>
    /// Looks for a property named <paramref name="propertyName"/> in the current object, returning
    /// whether or not such a property existed. When the property exists <paramref name="value"/>
    /// is assigned to the value of that property.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
    ///   </para>
    ///
    ///   <para>
    ///     If a property is defined multiple times for the same object, the last such definition is
    ///     what is matched.
    ///   </para>
    /// </remarks>
    /// <param name="propertyName">Name of the property to find.</param>
    /// <param name="value">Receives the value of the located property.</param>
    /// <returns>
    ///   <see langword="true"/> if the property was found, <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="propertyName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    /// <seealso cref="EnumerateObject"/>
    public bool TryGetProperty(string propertyName, out SourceResultElement value)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        return TryGetProperty(propertyName.AsSpan(), out value);
    }

    /// <summary>
    ///   Looks for a property named <paramref name="propertyName"/> in the current object, returning
    ///   whether or not such a property existed. When the property exists <paramref name="value"/>
    ///   is assigned to the value of that property.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
    ///   </para>
    ///
    ///   <para>
    ///     If a property is defined multiple times for the same object, the last such definition is
    ///     what is matched.
    ///   </para>
    /// </remarks>
    /// <param name="propertyName">Name of the property to find.</param>
    /// <param name="value">Receives the value of the located property.</param>
    /// <returns>
    ///   <see langword="true"/> if the property was found, <see langword="false"/> otherwise.
    /// </returns>
    /// <seealso cref="EnumerateObject"/>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetProperty(ReadOnlySpan<char> propertyName, out SourceResultElement value)
    {
        CheckValidInstance();

        return _parent.TryGetNamedPropertyValue(_index, propertyName, out value);
    }

    /// <summary>
    ///   Looks for a property named <paramref name="utf8PropertyName"/> in the current object, returning
    ///   whether or not such a property existed. When the property exists <paramref name="value"/>
    ///   is assigned to the value of that property.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Property name matching is performed as an ordinal, case-sensitive, comparison.
    ///   </para>
    ///
    ///   <para>
    ///     If a property is defined multiple times for the same object, the last such definition is
    ///     what is matched.
    ///   </para>
    /// </remarks>
    /// <param name="utf8PropertyName">
    ///   The UTF-8 (with no Byte-Order-Mark (BOM)) representation of the name of the property to return.
    /// </param>
    /// <param name="value">Receives the value of the located property.</param>
    /// <returns>
    ///   <see langword="true"/> if the property was found, <see langword="false"/> otherwise.
    /// </returns>
    /// <seealso cref="EnumerateObject"/>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out SourceResultElement value)
    {
        CheckValidInstance();

        return _parent.TryGetNamedPropertyValue(_index, utf8PropertyName, out value);
    }

    /// <summary>
    ///   Gets the value of the element as a <see cref="bool"/>.
    /// </summary>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>The value of the element as a <see cref="bool"/>.</returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is neither <see cref="JsonValueKind.True"/> or
    ///   <see cref="JsonValueKind.False"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool GetBoolean()
    {
        // CheckValidInstance is redundant. Asking for the type will
        // return None, which then throws the same exception in the return statement.

        var type = TokenType;

        return type switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            _ => ThrowJsonElementWrongTypeException(type)
        };

        static bool ThrowJsonElementWrongTypeException(JsonTokenType actualType)
        {
            throw new InvalidOperationException(string.Format(
                SourceResultElement_GetBoolean_JsonElementHasWrongType,
                nameof(Boolean),
                actualType.ToValueKind()))
            {
                Source = Rethrowable
            };
        }
    }

    /// <summary>
    /// Gets the value of the element as a <see cref="string"/>.
    /// </summary>
    /// <remarks>
    /// This method does not create a string representation of values other than JSON strings.
    /// </remarks>
    /// <returns>The value of the element as a <see cref="string"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value's <see cref="ValueKind"/> is neither <see cref="JsonValueKind.String"/> nor <see cref="JsonValueKind.Null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    /// <seealso cref="ToString"/>
    public string? GetString()
    {
        CheckValidInstance();

        return _parent.GetString(_index, JsonTokenType.String);
    }

    /// <summary>
    /// Gets the value of the element as a <see cref="string"/> and throws an error if it does not exist.
    /// </summary>
    /// <remarks>
    /// This method does not create a string representation of values other than JSON strings.
    /// </remarks>
    /// <returns>The value of the element as a <see cref="string"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// This value's <see cref="ValueKind"/> is neither <see cref="JsonValueKind.String"/> nor <see cref="JsonValueKind.Null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    /// <seealso cref="ToString"/>
    public string AssertString()
        => GetString() ?? throw new InvalidOperationException("The element value is null.");

    /// <summary>
    ///   Attempts to represent the current JSON number as an <see cref="sbyte"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as an <see cref="sbyte"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetSByte(out sbyte value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as an <see cref="sbyte"/>.
    /// </summary>
    /// <returns>The current JSON number as an <see cref="sbyte"/>.</returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as an <see cref="sbyte"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public sbyte GetSByte() => TryGetSByte(out var value) ? value : throw new FormatException();

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="byte"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="byte"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetByte(out byte value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="byte"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="byte"/>.</returns>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="byte"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public byte GetByte()
    {
        if (TryGetByte(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as an <see cref="short"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as an <see cref="short"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetInt16(out short value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as an <see cref="short"/>.
    /// </summary>
    /// <returns>The current JSON number as an <see cref="short"/>.</returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as an <see cref="short"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public short GetInt16()
    {
        if (TryGetInt16(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="ushort"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="ushort"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetUInt16(out ushort value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="ushort"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="ushort"/>.</returns>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="ushort"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public ushort GetUInt16()
    {
        if (TryGetUInt16(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as an <see cref="int"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as an <see cref="int"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetInt32(out int value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as an <see cref="int"/>.
    /// </summary>
    /// <returns>The current JSON number as an <see cref="int"/>.</returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as an <see cref="int"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public int GetInt32()
    {
        if (!TryGetInt32(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="uint"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="uint"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetUInt32(out uint value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="uint"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="uint"/>.</returns>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="uint"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public uint GetUInt32()
    {
        if (!TryGetUInt32(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="long"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="long"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetInt64(out long value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="long"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="long"/>.</returns>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="long"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public long GetInt64()
    {
        if (!TryGetInt64(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="ulong"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="ulong"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetUInt64(out ulong value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="ulong"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="ulong"/>.</returns>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="ulong"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public ulong GetUInt64()
    {
        if (!TryGetUInt64(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="double"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   <para>
    ///     This method does not parse the contents of a JSON string value.
    ///   </para>
    ///
    ///   <para>
    ///     On .NET Core this method does not return <see langword="false"/> for values larger than
    ///     <see cref="double.MaxValue"/> (or smaller than <see cref="double.MinValue"/>),
    ///     instead <see langword="true"/> is returned and <see cref="double.PositiveInfinity"/> (or
    ///     <see cref="double.NegativeInfinity"/>) is emitted.
    ///   </para>
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="double"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetDouble(out double value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="double"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="double"/>.</returns>
    /// <remarks>
    ///   <para>
    ///     This method does not parse the contents of a JSON string value.
    ///   </para>
    ///
    ///   <para>
    ///     On .NET Core this method returns <see cref="double.PositiveInfinity"/> (or
    ///     <see cref="double.NegativeInfinity"/>) for values larger than
    ///     <see cref="double.MaxValue"/> (or smaller than <see cref="double.MinValue"/>).
    ///   </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="double"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public double GetDouble()
    {
        if (!TryGetDouble(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="float"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   <para>
    ///     This method does not parse the contents of a JSON string value.
    ///   </para>
    ///
    ///   <para>
    ///     On .NET Core this method does not return <see langword="false"/> for values larger than
    ///     <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>),
    ///     instead <see langword="true"/> is returned and <see cref="float.PositiveInfinity"/> (or
    ///     <see cref="float.NegativeInfinity"/>) is emitted.
    ///   </para>
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="float"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public bool TryGetSingle(out float value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="float"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="float"/>.</returns>
    /// <remarks>
    ///   <para>
    ///     This method does not parse the contents of a JSON string value.
    ///   </para>
    ///
    ///   <para>
    ///     On .NET Core this method returns <see cref="float.PositiveInfinity"/> (or
    ///     <see cref="float.NegativeInfinity"/>) for values larger than
    ///     <see cref="float.MaxValue"/> (or smaller than <see cref="float.MinValue"/>).
    ///   </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="float"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public float GetSingle()
    {
        if (!TryGetSingle(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    /// <summary>
    ///   Attempts to represent the current JSON number as a <see cref="decimal"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    ///   <see langword="true"/> if the number can be represented as a <see cref="decimal"/>,
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    /// <seealso cref="GetRawText"/>
    public bool TryGetDecimal(out decimal value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_index, out value);
    }

    /// <summary>
    ///   Gets the current JSON number as a <see cref="decimal"/>.
    /// </summary>
    /// <returns>The current JSON number as a <see cref="decimal"/>.</returns>
    /// <remarks>
    ///   This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="FormatException">
    ///   The value cannot be represented as a <see cref="decimal"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    /// <seealso cref="GetRawText"/>
    public decimal GetDecimal()
    {
        if (!TryGetDecimal(out var value))
        {
            ThrowHelper.ThrowFormatException();
        }

        return value;
    }

    internal string GetPropertyName()
    {
        CheckValidInstance();

        return _parent.GetNameOfPropertyValue(_index);
    }

    internal ReadOnlySpan<byte> GetPropertyNameRaw()
    {
        CheckValidInstance();

        return _parent.GetPropertyNameRaw(_index);
    }

    /// <summary>
    ///   Gets the original input data backing this value, returning it as a <see cref="string"/>.
    /// </summary>
    /// <returns>
    ///   The original input data backing this value, returning it as a <see cref="string"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public string GetRawText()
    {
        CheckValidInstance();

        return _parent.GetRawValueAsString(_index);
    }

    internal string GetPropertyRawText()
    {
        CheckValidInstance();

        return _parent.GetPropertyRawValueAsString(_index);
    }

    internal ReadOnlySpan<byte> ValueSpan
    {
        get
        {
            CheckValidInstance();

            return _parent.GetRawValue(_index, includeQuotes: false);
        }
    }

    internal ValueRange GetValuePointer()
    {
        CheckValidInstance();

        return _parent.GetRawValuePointer(_index, includeQuotes: true);
    }

    /// <summary>
    ///   Compares <paramref name="text" /> to the string value of this element.
    /// </summary>
    /// <param name="text">The text to compare against.</param>
    /// <returns>
    ///   <see langword="true" /> if the string value of this element matches <paramref name="text"/>,
    ///   <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.String"/>.
    /// </exception>
    /// <remarks>
    ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
    ///   the result of calling <see cref="GetString" />, but avoids creating the string instance.
    /// </remarks>
    public bool ValueEquals(string? text)
    {
        // CheckValidInstance is done in the helper

        if (TokenType == JsonTokenType.Null)
        {
            return text == null;
        }

        return TextEqualsHelper(text.AsSpan(), isPropertyName: false);
    }

    /// <summary>
    ///   Compares the text represented by <paramref name="utf8Text" /> to the string value of this element.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to compare against.</param>
    /// <returns>
    ///   <see langword="true" /> if the string value of this element has the same UTF-8 encoding as
    ///   <paramref name="utf8Text" />, <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.String"/>.
    /// </exception>
    /// <remarks>
    ///   This method is functionally equal to doing an ordinal comparison of the string produced by UTF-8 decoding
    ///   <paramref name="utf8Text" /> with the result of calling <see cref="GetString" />, but avoids creating the
    ///   string instances.
    /// </remarks>
    public bool ValueEquals(ReadOnlySpan<byte> utf8Text)
    {
        // CheckValidInstance is done in the helper

        if (TokenType == JsonTokenType.Null)
        {
            // This is different from Length == 0, in that it tests true for null, but false for ""
#pragma warning disable CA2265
            return utf8Text.Slice(0, 0) == default;
#pragma warning restore CA2265
        }

        return TextEqualsHelper(utf8Text, isPropertyName: false, shouldUnescape: true);
    }

    /// <summary>
    ///   Compares <paramref name="text" /> to the string value of this element.
    /// </summary>
    /// <param name="text">The text to compare against.</param>
    /// <returns>
    ///   <see langword="true" /> if the string value of this element matches <paramref name="text"/>,
    ///   <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.String"/>.
    /// </exception>
    /// <remarks>
    ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
    ///   the result of calling <see cref="GetString" />, but avoids creating the string instance.
    /// </remarks>
    public bool ValueEquals(ReadOnlySpan<char> text)
    {
        // CheckValidInstance is done in the helper

        if (TokenType == JsonTokenType.Null)
        {
            // This is different than Length == 0, in that it tests true for null, but false for ""
#pragma warning disable CA2265
            return text.Slice(0, 0) == default;
#pragma warning restore CA2265
        }

        return TextEqualsHelper(text, isPropertyName: false);
    }

    internal bool TextEqualsHelper(ReadOnlySpan<byte> utf8Text, bool isPropertyName, bool shouldUnescape)
    {
        CheckValidInstance();

        return _parent.TextEquals(_index, utf8Text, isPropertyName, shouldUnescape);
    }

    internal bool TextEqualsHelper(ReadOnlySpan<char> text, bool isPropertyName)
    {
        CheckValidInstance();

        return _parent.TextEquals(_index, text, isPropertyName);
    }

    /// <summary>
    ///   Get an enumerator to enumerate the values in the JSON array represented by this JsonElement.
    /// </summary>
    /// <returns>
    ///   An enumerator to enumerate the values in the JSON array represented by this JsonElement.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Array"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public ArrayEnumerator EnumerateArray()
    {
        CheckValidInstance();

        var tokenType = TokenType;

        if (tokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException(string.Format(
                "The requested operation requires an element of type '{0}', but the target element has type '{1}'.",
                JsonTokenType.StartArray,
                tokenType))
            {
                Source = Rethrowable
            };
        }

        return new ArrayEnumerator(this);
    }

    /// <summary>
    ///   Get an enumerator to enumerate the properties in the JSON object represented by this JsonElement.
    /// </summary>
    /// <returns>
    ///   An enumerator to enumerate the properties in the JSON object represented by this JsonElement.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public ObjectEnumerator EnumerateObject()
    {
        CheckValidInstance();

        var tokenType = TokenType;

        if (tokenType != JsonTokenType.StartObject)
        {
            // TODO : rework exception
            throw new InvalidOperationException();
            // ThrowHelper.ThrowJsonElementWrongTypeException(JsonTokenType.StartObject, tokenType);
        }

        return new ObjectEnumerator(this);
    }

    /*
    /// <summary>
    ///   Gets a string representation for the current value appropriate to the value type.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     For JsonElement built from <see cref="JsonDocument"/>:
    ///   </para>
    ///
    ///   <para>
    ///     For <see cref="JsonValueKind.Null"/>, <see cref="string.Empty"/> is returned.
    ///   </para>
    ///
    ///   <para>
    ///     For <see cref="JsonValueKind.True"/>, <see cref="bool.TrueString"/> is returned.
    ///   </para>
    ///
    ///   <para>
    ///     For <see cref="JsonValueKind.False"/>, <see cref="bool.FalseString"/> is returned.
    ///   </para>
    ///
    ///   <para>
    ///     For <see cref="JsonValueKind.String"/>, the value of <see cref="GetString"/>() is returned.
    ///   </para>
    ///
    ///   <para>
    ///     For other types, the value of <see cref="GetRawText"/>() is returned.
    ///   </para>
    /// </remarks>
    /// <returns>
    ///   A string representation for the current value appropriate to the value type.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public override string ToString()
    {
        switch (TokenType)
        {
            case JsonTokenType.None:
            case JsonTokenType.Null:
                return string.Empty;
            case JsonTokenType.True:
                return bool.TrueString;
            case JsonTokenType.False:
                return bool.FalseString;
            case JsonTokenType.Number:
            case JsonTokenType.StartArray:
            case JsonTokenType.StartObject:
            {
                // null parent should have hit the None case
                Debug.Assert(_parent != null);
                return _parent.GetRawValueAsString(_index);
            }
            case JsonTokenType.String:
                return GetString()!;
            case JsonTokenType.Comment:
            case JsonTokenType.EndArray:
            case JsonTokenType.EndObject:
            default:
                Debug.Fail($"No handler for {nameof(JsonTokenType)}.{TokenType}");
                return string.Empty;
        }
    }

    /// <summary>
    ///   Get a JsonElement which can be safely stored beyond the lifetime of the
    ///   original <see cref="JsonDocument"/>.
    /// </summary>
    /// <returns>
    ///   A JsonElement which can be safely stored beyond the lifetime of the
    ///   original <see cref="JsonDocument"/>.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     If this JsonElement is itself the output of a previous call to Clone, or
    ///     a value contained within another JsonElement which was the output of a previous
    ///     call to Clone, this method results in no additional memory allocation.
    ///   </para>
    /// </remarks>
    public JsonElement Clone()
    {
        CheckValidInstance();

        if (!_parent.IsDisposable)
        {
            return this;
        }

        return _parent.CloneElement(_index);
    }
    */

    private void CheckValidInstance()
    {
        if (_parent == null)
        {
            throw new InvalidOperationException();
        }
    }

    internal readonly int MetadataDbIndex => _index;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"ValueKind = {ValueKind} : \"{ToString()}\"";
}
