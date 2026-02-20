using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Text.Json;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct CompositeResultElement
{
    private readonly CompositeResultDocument _parent;
    private readonly CompositeResultDocument.Cursor _cursor;

    internal CompositeResultElement(CompositeResultDocument parent, CompositeResultDocument.Cursor cursor)
    {
        // parent is usually not null, but the Current property
        // on the enumerators (when initialized as `default`) can
        // get here with a null.
        _parent = parent;
        _cursor = cursor;
    }

    /// <summary>
    /// Writes this element as JSON to the specified buffer writer.
    /// </summary>
    /// <param name="writer">The buffer writer to write to.</param>
    /// <param name="indented">
    /// <c>true</c> to write indented JSON; otherwise, <c>false</c>.
    /// </param>
    public void WriteTo(IBufferWriter<byte> writer, bool indented = false)
    {
        var options = new JsonWriterOptions { Indented = indented };
        var jsonWriter = new JsonWriter(writer, options);
        var formatter = new CompositeResultDocument.RawJsonFormatter(
            _parent,
            jsonWriter);
        var row = _parent._metaDb.Get(_cursor);
        formatter.WriteValue(_cursor, row);
    }

    /// <summary>
    /// Gets the internal meta-db cursor.
    /// </summary>
    internal CompositeResultDocument.Cursor Cursor => _cursor;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ElementTokenType TokenType => _parent?.GetElementTokenType(_cursor) ?? ElementTokenType.None;

    /// <summary>
    /// Gets the <see cref="JsonValueKind"/> of this element.
    /// </summary>
    public JsonValueKind ValueKind => TokenType.ToValueKind();

    /// <summary>
    /// Gets the element at the specified index when the current element is an array.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <exception cref="InvalidOperationException">
    /// This element's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Array"/>.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="index"/> is not in the range [0, <see cref="GetArrayLength"/>()).
    /// </exception>
    public CompositeResultElement this[int index]
    {
        get
        {
            CheckValidInstance();

            return _parent.GetArrayIndexElement(_cursor, index);
        }
    }

    /// <summary>
    /// Gets the operation this element belongs to.
    /// </summary>
    public Operation Operation
    {
        get
        {
            CheckValidInstance();

            return _parent.GetOperation();
        }
    }

    /// <summary>
    /// Gets the <see cref="SelectionSet"/> if this element represents the data of a selection set;
    /// otherwise, <c>null</c>.
    /// </summary>
    public SelectionSet? SelectionSet
    {
        get
        {
            CheckValidInstance();

            return _parent.GetSelectionSet(_cursor);
        }
    }

    /// <summary>
    /// Gets the <see cref="Selection"/> if this element represents the data of a field selection;
    /// otherwise, <c>null</c>.
    /// </summary>
    public Selection? Selection
    {
        get
        {
            CheckValidInstance();

            if (_cursor == CompositeResultDocument.Cursor.Zero)
            {
                return null;
            }

            // note: the selection is stored on the property not on the value.
            return _parent.GetSelection(_cursor - 1);
        }
    }

    /// <summary>
    /// Gets the <see cref="IType"/> if this element represents the data of a field selection;
    /// otherwise, <c>null</c>.
    /// </summary>
    public IType? Type
    {
        get
        {
            if (_cursor == CompositeResultDocument.Cursor.Zero)
            {
                return null;
            }

            var selection = Selection;

            if (selection is not null)
            {
                return selection.Type;
            }

            var type = Parent.Type;

            if (type?.IsListType() == true)
            {
                return type.ElementType();
            }

            return null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this element has been invalidated during null propagation.
    /// </summary>
    public bool IsInvalidated
    {
        get
        {
            CheckValidInstance();

            return _parent.IsInvalidated(_cursor);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this element is either null or was invalidated during null propagation.
    /// </summary>
    public bool IsNullOrInvalidated
    {
        get
        {
            if (_parent is null)
            {
                return true;
            }

            return _parent.IsNullOrInvalidated(_cursor);
        }
    }

    /// <summary>
    /// Gets the path to this element within the result document.
    /// </summary>
    public Path Path
    {
        get
        {
            CheckValidInstance();

            return _parent.CreatePath(_cursor);
        }
    }

    /// <summary>
    /// Gets the parent element that contains this element.
    /// </summary>
    public CompositeResultElement Parent
    {
        get
        {
            CheckValidInstance();

            return _parent.GetParent(_cursor);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this element is nullable according to the GraphQL type system.
    /// </summary>
    public bool IsNullable
    {
        get
        {
            CheckValidInstance();

            if (_cursor == CompositeResultDocument.Cursor.Zero)
            {
                return false;
            }

            return Type?.IsNullableType() ?? true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this element represents internal data
    /// that is required for processing and must not be written to the GraphQL response.
    /// </summary>
    public bool IsInternal
    {
        get
        {
            CheckValidInstance();

            return _parent.IsInternalProperty(_cursor);
        }
    }

    /// <summary>
    /// Gets the <see cref="SelectionSet"/> for this element.
    /// </summary>
    /// <returns>
    /// The <see cref="SelectionSet"/> instance.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This element does not represent the data of a selection set.
    /// </exception>
    public SelectionSet AssertSelectionSet()
    {
        var selectionSet = SelectionSet;

        if (selectionSet is null)
        {
            throw new InvalidOperationException("The selection set is null.") { Source = Rethrowable };
        }

        return selectionSet;
    }

    /// <summary>
    /// Gets the <see cref="Selection"/> for this element.
    /// </summary>
    /// <returns>
    /// The <see cref="Selection"/> instance.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This element does not represent the data of a field selection.
    /// </exception>
    public Selection AssertSelection()
    {
        var selection = Selection;

        if (selection is null)
        {
            throw new InvalidOperationException("The selection set is null.") { Source = Rethrowable };
        }

        return selection;
    }

    /// <summary>
    /// Gets the <see cref="IType"/> for this element.
    /// </summary>
    /// <returns>
    /// The <see cref="IType"/> instance.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This element does not represent the data of a field selection.
    /// </exception>
    public IType AssertType()
    {
        var type = Type;

        if (type is null)
        {
            throw new InvalidOperationException("The type is null.") { Source = Rethrowable };
        }

        return type;
    }

    /// <summary>
    /// Marks this element as invalidated, which occurs during null propagation
    /// when a non-nullable field returns null.
    /// </summary>
    public void Invalidate()
    {
        CheckValidInstance();

        _parent.Invalidate(_cursor);
    }

    /// <summary>
    /// Gets the number of elements contained within the current array element.
    /// </summary>
    /// <returns>
    /// The number of elements in the array.
    /// </returns>
    public int GetArrayLength()
    {
        CheckValidInstance();

        return _parent.GetArrayLength(_cursor);
    }

    /// <summary>
    /// Gets the number of properties contained within the current object element.
    /// </summary>
    /// <returns>
    /// The number of properties in the object.
    /// </returns>
    public int GetPropertyCount()
    {
        CheckValidInstance();

        return _parent.GetPropertyCount(_cursor);
    }

    /// <summary>
    /// Gets a property by name when the current element is an object.
    /// </summary>
    /// <param name="propertyName">The name of the property to find.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// No property with the specified name was found.
    /// </exception>
    public CompositeResultElement GetProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (TryGetProperty(propertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    /// <summary>
    /// Gets a property by UTF-8 encoded name when the current element is an object.
    /// </summary>
    /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to find.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// No property with the specified name was found.
    /// </exception>
    public CompositeResultElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
    {
        if (TryGetProperty(utf8PropertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    /// <summary>
    /// Attempts to get a property by name when the current element is an object.
    /// </summary>
    /// <param name="propertyName">The name of the property to find.</param>
    /// <param name="value">
    /// When this method returns, contains the property value if found; otherwise, the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the property was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetProperty(string propertyName, out CompositeResultElement value)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        return _parent.TryGetNamedPropertyValue(_cursor, propertyName, out value);
    }

    /// <summary>
    /// Attempts to get a property by UTF-8 encoded name when the current element is an object.
    /// </summary>
    /// <param name="utf8PropertyName">The UTF-8 encoded name of the property to find.</param>
    /// <param name="value">
    /// When this method returns, contains the property value if found; otherwise, the default value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the property was found; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out CompositeResultElement value)
    {
        CheckValidInstance();

        return _parent.TryGetNamedPropertyValue(_cursor, utf8PropertyName, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="bool"/>.
    /// </summary>
    /// <returns>The boolean value.</returns>
    /// <exception cref="InvalidOperationException">
    /// This element's <see cref="ValueKind"/> is not <see cref="JsonValueKind.True"/> or <see cref="JsonValueKind.False"/>.
    /// </exception>
    public bool GetBoolean()
    {
        var type = TokenType;

        return type switch
        {
            ElementTokenType.True => true,
            ElementTokenType.False => false,
            _ => ThrowJsonElementWrongTypeException(type)
        };

        static bool ThrowJsonElementWrongTypeException(ElementTokenType actualType)
        {
            throw new InvalidOperationException(string.Format(
                CompositeResultElement_GetBoolean_JsonElementHasWrongType,
                nameof(Boolean),
                actualType.ToValueKind()))
            {
                Source = Rethrowable
            };
        }
    }

    /// <summary>
    /// Gets the value as a <see cref="string"/>.
    /// </summary>
    /// <returns>The string value, or <c>null</c> if this element is a JSON null.</returns>
    public string? GetString()
    {
        CheckValidInstance();

        return _parent.GetString(_cursor, ElementTokenType.String);
    }

    /// <summary>
    /// Gets the value as a non-null <see cref="string"/>.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">
    /// This element is a JSON null.
    /// </exception>
    public string AssertString()
    {
        CheckValidInstance();

        return _parent.GetRequiredString(_cursor, ElementTokenType.String);
    }

    /// <summary>
    /// Attempts to get the value as an <see cref="sbyte"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetSByte(out sbyte value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as an <see cref="sbyte"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as an <see cref="sbyte"/>.</exception>
    public sbyte GetSByte() => TryGetSByte(out var value) ? value : throw new FormatException();

    /// <summary>
    /// Attempts to get the value as a <see cref="byte"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetByte(out byte value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="byte"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="byte"/>.</exception>
    public byte GetByte()
    {
        if (TryGetByte(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="short"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetInt16(out short value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="short"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="short"/>.</exception>
    public short GetInt16()
    {
        if (TryGetInt16(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="ushort"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetUInt16(out ushort value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="ushort"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="ushort"/>.</exception>
    public ushort GetUInt16()
    {
        if (TryGetUInt16(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    /// <summary>
    /// Attempts to get the value as an <see cref="int"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetInt32(out int value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as an <see cref="int"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as an <see cref="int"/>.</exception>
    public int GetInt32()
    {
        if (!TryGetInt32(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="uint"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetUInt32(out uint value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="uint"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="uint"/>.</exception>
    public uint GetUInt32()
    {
        if (!TryGetUInt32(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="long"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetInt64(out long value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="long"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="long"/>.</exception>
    public long GetInt64()
    {
        if (!TryGetInt64(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="ulong"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetUInt64(out ulong value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="ulong"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="ulong"/>.</exception>
    public ulong GetUInt64()
    {
        if (!TryGetUInt64(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="double"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetDouble(out double value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="double"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="double"/>.</exception>
    public double GetDouble()
    {
        if (!TryGetDouble(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="float"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetSingle(out float value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="float"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="float"/>.</exception>
    public float GetSingle()
    {
        if (!TryGetSingle(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the value as a <see cref="decimal"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the parsed value.</param>
    /// <returns><c>true</c> if the value could be parsed; otherwise, <c>false</c>.</returns>
    public bool TryGetDecimal(out decimal value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    /// <summary>
    /// Gets the value as a <see cref="decimal"/>.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="FormatException">The value cannot be parsed as a <see cref="decimal"/>.</exception>
    public decimal GetDecimal()
    {
        if (!TryGetDecimal(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    internal string GetPropertyName()
    {
        CheckValidInstance();

        return _parent.GetNameOfPropertyValue(_cursor);
    }

    internal ReadOnlySpan<byte> GetPropertyNameRaw()
    {
        CheckValidInstance();

        return _parent.GetPropertyNameRaw(_cursor);
    }

    /// <summary>
    /// Gets the raw JSON text representing this element.
    /// </summary>
    /// <returns>The raw JSON text.</returns>
    public string GetRawText()
    {
        CheckValidInstance();

        return _parent.GetRawValueAsString(_cursor);
    }

    internal ReadOnlySpan<byte> GetRawValue(bool includeQuotes = true)
    {
        CheckValidInstance();

        return _parent.GetRawValue(_cursor, includeQuotes: true);
    }

    /// <summary>
    /// Compares the text of this element to the specified string.
    /// </summary>
    /// <param name="text">The text to compare against.</param>
    /// <returns>
    /// <c>true</c> if this element's value equals the specified text; otherwise, <c>false</c>.
    /// </returns>
    public bool ValueEquals(string? text)
    {
        if (TokenType == ElementTokenType.Null)
        {
            return text == null;
        }

        return TextEqualsHelper(text.AsSpan(), isPropertyName: false);
    }

    /// <summary>
    /// Compares the text of this element to the specified UTF-8 encoded text.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to compare against.</param>
    /// <returns>
    /// <c>true</c> if this element's value equals the specified text; otherwise, <c>false</c>.
    /// </returns>
    public bool ValueEquals(ReadOnlySpan<byte> utf8Text)
    {
        if (TokenType == ElementTokenType.Null)
        {
#pragma warning disable CA2265
            return utf8Text[..0] == default;
#pragma warning restore CA2265
        }

        return TextEqualsHelper(utf8Text, isPropertyName: false, shouldUnescape: true);
    }

    /// <summary>
    /// Compares the text of this element to the specified character span.
    /// </summary>
    /// <param name="text">The text to compare against.</param>
    /// <returns>
    /// <c>true</c> if this element's value equals the specified text; otherwise, <c>false</c>.
    /// </returns>
    public bool ValueEquals(ReadOnlySpan<char> text)
    {
        if (TokenType == ElementTokenType.Null)
        {
#pragma warning disable CA2265
            return text[..0] == default;
#pragma warning restore CA2265
        }

        return TextEqualsHelper(text, isPropertyName: false);
    }

    internal bool TextEqualsHelper(ReadOnlySpan<byte> utf8Text, bool isPropertyName, bool shouldUnescape)
    {
        CheckValidInstance();

        return _parent.TextEquals(_cursor, utf8Text, isPropertyName, shouldUnescape);
    }

    internal bool TextEqualsHelper(ReadOnlySpan<char> text, bool isPropertyName)
    {
        CheckValidInstance();

        return _parent.TextEquals(_cursor, text, isPropertyName);
    }

    internal string GetPropertyRawText()
    {
        CheckValidInstance();

        return _parent.GetPropertyRawValueAsString(_cursor);
    }

    /// <summary>
    /// Gets an enumerator to enumerate the elements of this array.
    /// </summary>
    /// <returns>An enumerator for the array elements.</returns>
    /// <exception cref="InvalidOperationException">
    /// This element's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Array"/>.
    /// </exception>
    public ArrayEnumerator EnumerateArray()
    {
        CheckValidInstance();

        var tokenType = TokenType;

        if (tokenType != ElementTokenType.StartArray)
        {
            throw new InvalidOperationException(string.Format(
                "The requested operation requires an element of type '{0}', but the target element has type '{1}'.",
                ElementTokenType.StartArray,
                tokenType))
            {
                Source = Rethrowable
            };
        }

        return new ArrayEnumerator(this);
    }

    /// <summary>
    /// Gets an enumerator to enumerate the properties of this object.
    /// </summary>
    /// <returns>An enumerator for the object properties.</returns>
    /// <exception cref="InvalidOperationException">
    /// This element's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Object"/>.
    /// </exception>
    public ObjectEnumerator EnumerateObject()
    {
        CheckValidInstance();

        var tokenType = TokenType;

        if (tokenType is not ElementTokenType.StartObject)
        {
            throw new InvalidOperationException(string.Format(
                "The requested operation requires an element of type '{0}', but the target element has type '{1}'.",
                ElementTokenType.StartObject,
                tokenType));
        }

        return new ObjectEnumerator(this);
    }

    internal void SetObjectValue(SelectionSet selectionSet)
    {
        CheckValidInstance();

        ArgumentNullException.ThrowIfNull(selectionSet);

        var obj = _parent.CreateObject(_cursor, selectionSet: selectionSet);
        _parent.AssignCompositeValue(this, obj);
    }

    internal void SetArrayValue(int length)
    {
        CheckValidInstance();

        ArgumentOutOfRangeException.ThrowIfNegative(length);

        var arr = _parent.CreateArray(_cursor, length);
        _parent.AssignCompositeValue(this, arr);
    }

    internal void SetLeafValue(SourceResultElement source)
    {
        CheckValidInstance();

        _parent.AssignSourceValue(this, source);
    }

    internal void SetNullValue()
    {
        CheckValidInstance();

        _parent.AssignNullValue(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        switch (TokenType)
        {
            case ElementTokenType.None:
            case ElementTokenType.Null:
                return string.Empty;

            case ElementTokenType.True:
                return bool.TrueString;

            case ElementTokenType.False:
                return bool.FalseString;

            case ElementTokenType.Number:
            case ElementTokenType.StartArray:
            case ElementTokenType.StartObject:
                Debug.Assert(_parent != null);
                return _parent.GetRawValueAsString(_cursor);

            case ElementTokenType.String:
                return GetString()!;

            case ElementTokenType.Comment:
            case ElementTokenType.EndArray:
            case ElementTokenType.EndObject:
            default:
                Debug.Fail($"No handler for {nameof(JsonTokenType)}.{TokenType}");
                return string.Empty;
        }
    }

    private void CheckValidInstance()
    {
        if (_parent == null)
        {
            throw new InvalidOperationException();
        }
    }
}
