using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

#pragma warning disable CS1574, CS1584, CS1581, CS1580

namespace HotChocolate.Fusion.Text.Json;

public readonly partial struct CompositeResultElement : IRawJsonFormatter
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

    public void WriteTo(IBufferWriter<byte> writer, bool indented = false)
    {
        var formatter = new CompositeResultDocument.RawJsonFormatter(_parent, writer, indented);

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
    ///   The <see cref="JsonValueKind"/> that the value is.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public JsonValueKind ValueKind => TokenType.ToValueKind();

    /// <summary>
    ///   Get the value at a specified index when the current value is a
    ///   <see cref="JsonValueKind.Array"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Array"/>.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    ///   <paramref name="index"/> is not in the range [0, <see cref="GetArrayLength"/>()).
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The parent <see cref="JsonDocument"/> has been disposed.
    /// </exception>
    public CompositeResultElement this[int index]
    {
        get
        {
            CheckValidInstance();

            return _parent.GetArrayIndexElement(_cursor, index);
        }
    }

    public Operation Operation
    {
        get
        {
            CheckValidInstance();

            return _parent.GetOperation();
        }
    }

    public SelectionSet? SelectionSet
    {
        get
        {
            CheckValidInstance();

            return _parent.GetSelectionSet(_cursor);
        }
    }

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

    public bool IsInvalidated
    {
        get
        {
            CheckValidInstance();

            return _parent.IsInvalidated(_cursor);
        }
    }

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

    public Path Path
    {
        get
        {
            CheckValidInstance();

            return _parent.CreatePath(_cursor);
        }
    }

    public CompositeResultElement Parent
    {
        get
        {
            CheckValidInstance();

            return _parent.GetParent(_cursor);
        }
    }

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

    public bool IsInternal
    {
        get
        {
            CheckValidInstance();

            return _parent.IsInternalProperty(_cursor);
        }
    }

    public SelectionSet AssertSelectionSet()
    {
        var selectionSet = SelectionSet;

        if (selectionSet is null)
        {
            throw new InvalidOperationException("The selection set is null.") { Source = Rethrowable };
        }

        return selectionSet;
    }

    public Selection AssertSelection()
    {
        var selection = Selection;

        if (selection is null)
        {
            throw new InvalidOperationException("The selection set is null.") { Source = Rethrowable };
        }

        return selection;
    }

    public IType AssertType()
    {
        var type = Type;

        if (type is null)
        {
            throw new InvalidOperationException("The type is null.") { Source = Rethrowable };
        }

        return type;
    }

    public void Invalidate()
    {
        CheckValidInstance();

        _parent.Invalidate(_cursor);
    }

    /// <summary>
    /// Get the number of values contained within the current array value.
    /// </summary>
    public int GetArrayLength()
    {
        CheckValidInstance();

        return _parent.GetArrayLength(_cursor);
    }

    /// <summary>
    ///   Get the number of properties contained within the current object value.
    /// </summary>
    public int GetPropertyCount()
    {
        CheckValidInstance();

        return _parent.GetPropertyCount(_cursor);
    }

    public CompositeResultElement GetProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (TryGetProperty(propertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    public CompositeResultElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
    {
        if (TryGetProperty(utf8PropertyName, out var property))
        {
            return property;
        }

        throw new KeyNotFoundException();
    }

    public bool TryGetProperty(string propertyName, out CompositeResultElement value)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        return _parent.TryGetNamedPropertyValue(_cursor, propertyName, out value);
    }

    public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out CompositeResultElement value)
    {
        CheckValidInstance();

        return _parent.TryGetNamedPropertyValue(_cursor, utf8PropertyName, out value);
    }

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

    public string? GetString()
    {
        CheckValidInstance();

        return _parent.GetString(_cursor, ElementTokenType.String);
    }

    public string AssertString()
    {
        CheckValidInstance();

        return _parent.GetRequiredString(_cursor, ElementTokenType.String);
    }

    public bool TryGetSByte(out sbyte value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public sbyte GetSByte() => TryGetSByte(out var value) ? value : throw new FormatException();

    public bool TryGetByte(out byte value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public byte GetByte()
    {
        if (TryGetByte(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    public bool TryGetInt16(out short value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public short GetInt16()
    {
        if (TryGetInt16(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    public bool TryGetUInt16(out ushort value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public ushort GetUInt16()
    {
        if (TryGetUInt16(out var value))
        {
            return value;
        }

        throw new FormatException();
    }

    public bool TryGetInt32(out int value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public int GetInt32()
    {
        if (!TryGetInt32(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    public bool TryGetUInt32(out uint value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public uint GetUInt32()
    {
        if (!TryGetUInt32(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    public bool TryGetInt64(out long value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public long GetInt64()
    {
        if (!TryGetInt64(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    public bool TryGetUInt64(out ulong value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public ulong GetUInt64()
    {
        if (!TryGetUInt64(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    public bool TryGetDouble(out double value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public double GetDouble()
    {
        if (!TryGetDouble(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    public bool TryGetSingle(out float value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

    public float GetSingle()
    {
        if (!TryGetSingle(out var value))
        {
            ThrowHelper.FormatException();
        }

        return value;
    }

    public bool TryGetDecimal(out decimal value)
    {
        CheckValidInstance();

        return _parent.TryGetValue(_cursor, out value);
    }

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

    public bool ValueEquals(string? text)
    {
        if (TokenType == ElementTokenType.Null)
        {
            return text == null;
        }

        return TextEqualsHelper(text.AsSpan(), isPropertyName: false);
    }

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
