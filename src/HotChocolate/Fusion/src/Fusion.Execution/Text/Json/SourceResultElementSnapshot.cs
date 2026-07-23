using System.Diagnostics;
using System.Text.Json;
using static HotChocolate.Fusion.Properties.FusionExecutionResources;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// A transient, stack-only view of a source result element that carries the element's
/// decoded metadata row so repeated row lookups are avoided. Instances are obtained via
/// <see cref="SourceResultElement.CreateSnapshot"/> and are only valid while the parent
/// <see cref="SourceResultDocument"/> is alive.
/// </summary>
internal readonly ref struct SourceResultElementSnapshot
{
    internal readonly SourceResultDocument _parent;
    internal readonly SourceResultDocument.Cursor _cursor;
    internal readonly SourceResultDocument.DbRow _row;

    internal SourceResultElementSnapshot(
        SourceResultDocument parent,
        SourceResultDocument.Cursor cursor,
        SourceResultDocument.DbRow row)
    {
        _parent = parent;
        _cursor = cursor;
        _row = row;
    }

    /// <summary>
    /// Gets the <see cref="SourceResultElement"/> this snapshot was created from.
    /// </summary>
    public SourceResultElement Element => new(_parent, _cursor);

    internal JsonTokenType TokenType => _row.TokenType;

    /// <summary>
    /// The <see cref="JsonValueKind"/> the value is.
    /// </summary>
    public JsonValueKind ValueKind => _row.TokenType.ToValueKind();

    /// <summary>
    /// Get the value at a specified index when the current value is a
    /// <see cref="JsonValueKind.Array"/>.
    /// </summary>
    /// <param name="index">Zero-based index within the array.</param>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON array.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// <paramref name="index"/> is outside the range [0, <see cref="GetArrayLength"/>()).
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement this[int index] => Element[index];

    /// <summary>
    /// Gets the number of elements in the current array.
    /// </summary>
    /// <returns>The number of elements in the array.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON array.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public int GetArrayLength() => _parent.GetArrayLength(_row);

    /// <summary>
    /// Gets the number of properties in the current object.
    /// </summary>
    /// <returns>The number of properties in the object.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public int GetPropertyCount() => _parent.GetPropertyCount(_row);

    /// <summary>
    /// Gets the value of a required property with name <paramref name="propertyName"/>.
    /// </summary>
    /// <remarks>
    /// Property name matching is ordinal and case-sensitive. If the property occurs more than once,
    /// the last occurrence wins.
    /// </remarks>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The value of the requested property.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No property was found with the requested name.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="propertyName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement GetProperty(string propertyName)
        => Element.GetProperty(propertyName);

    /// <summary>
    /// Gets the value of a required property with name <paramref name="propertyName"/>.
    /// </summary>
    /// <remarks>
    /// Property name matching is ordinal and case-sensitive. If the property occurs more than once,
    /// the last occurrence wins.
    /// </remarks>
    /// <param name="propertyName">The property name (as UTF-16 span).</param>
    /// <returns>The value of the requested property.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No property was found with the requested name.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement GetProperty(ReadOnlySpan<char> propertyName)
        => Element.GetProperty(propertyName);

    /// <summary>
    /// Gets the value of a required property with name <paramref name="utf8PropertyName"/>.
    /// </summary>
    /// <remarks>
    /// Property name matching is ordinal and case-sensitive. If the property occurs more than once,
    /// the last occurrence wins.
    /// </remarks>
    /// <param name="utf8PropertyName">The property name (as UTF-8 bytes without BOM).</param>
    /// <returns>The value of the requested property.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// No property was found with the requested name.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public SourceResultElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
        => Element.GetProperty(utf8PropertyName);

    /// <summary>
    /// Tries to get the value of property <paramref name="propertyName"/> without throwing.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">Receives the property value when found.</param>
    /// <returns><see langword="true"/> if the property exists; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="propertyName"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public bool TryGetProperty(string propertyName, out SourceResultElement value)
        => Element.TryGetProperty(propertyName, out value);

    /// <summary>
    /// Tries to get the value of property <paramref name="propertyName"/> without throwing.
    /// </summary>
    /// <param name="propertyName">The property name (UTF-16 span).</param>
    /// <param name="value">Receives the property value when found.</param>
    /// <returns><see langword="true"/> if the property exists; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public bool TryGetProperty(ReadOnlySpan<char> propertyName, out SourceResultElement value)
        => Element.TryGetProperty(propertyName, out value);

    /// <summary>
    /// Tries to get the value of property <paramref name="utf8PropertyName"/> without throwing.
    /// </summary>
    /// <param name="utf8PropertyName">The property name (UTF-8 bytes without BOM).</param>
    /// <param name="value">Receives the property value when found.</param>
    /// <returns><see langword="true"/> if the property exists; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out SourceResultElement value)
        => Element.TryGetProperty(utf8PropertyName, out value);

    /// <summary>
    /// Gets the value as a <see cref="bool"/>.
    /// </summary>
    /// <remarks>This method does not parse JSON strings.</remarks>
    /// <returns>The value as <see cref="bool"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value kind is neither <see cref="JsonValueKind.True"/> nor <see cref="JsonValueKind.False"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public bool GetBoolean()
    {
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
            { Source = Rethrowable };
        }
    }

    /// <summary>
    /// Gets the value as a <see cref="string"/>.
    /// </summary>
    /// <remarks>This method does not create string representations for non-string JSON values.</remarks>
    /// <returns>The string value, or <see langword="null"/> for JSON null.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value kind is neither <see cref="JsonValueKind.String"/> nor <see cref="JsonValueKind.Null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public string? GetString() => _parent.GetString(_row, JsonTokenType.String);

    /// <summary>
    /// Gets the value as a non-null <see cref="string"/>, throwing if null.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">
    /// The value kind is neither <see cref="JsonValueKind.String"/> nor <see cref="JsonValueKind.Null"/>,
    /// or the value is null.
    /// </exception>
    public string AssertString()
        => GetString() ?? throw new InvalidOperationException("The element value is null.");

    /// <summary>Tries to get the current JSON number as an <see cref="sbyte"/> without throwing.</summary>
    /// <param name="value">Receives the parsed value if successful.</param>
    /// <returns><see langword="true"/> if the value was read; otherwise <see langword="false"/>.</returns>
    /// <remarks>This method does not parse JSON strings.</remarks>
    /// <exception cref="InvalidOperationException">The value kind is not Number.</exception>
    /// <exception cref="ObjectDisposedException">Parent <see cref="SourceResultDocument"/> disposed.</exception>
    public bool TryGetSByte(out sbyte value) => Element.TryGetSByte(out value);

    /// <summary>Gets the current JSON number as an <see cref="sbyte"/>.</summary>
    /// <returns>The parsed value.</returns>
    /// <remarks>This method does not parse JSON strings.</remarks>
    /// <exception cref="InvalidOperationException">The value kind is not Number.</exception>
    /// <exception cref="FormatException">The value is out of range for <see cref="sbyte"/>.</exception>
    /// <exception cref="ObjectDisposedException">Parent <see cref="SourceResultDocument"/> disposed.</exception>
    public sbyte GetSByte() => Element.GetSByte();

    /// <summary>
    /// Attempts to represent the current JSON number as a <see cref="byte"/>.
    /// </summary>
    /// <param name="value">Receives the value.</param>
    /// <remarks>
    /// This method does not parse the contents of a JSON string value.
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> if the number can be represented as a <see cref="byte"/>,
    /// <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// This value's <see cref="ValueKind"/> is not <see cref="JsonValueKind.Number"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public bool TryGetByte(out byte value) => Element.TryGetByte(out value);

    /// <summary>Gets the current JSON number as a <see cref="byte"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="byte"/>.</exception>
    public byte GetByte() => Element.GetByte();

    /// <summary>Tries to get the current JSON number as a <see cref="short"/> without throwing.</summary>
    public bool TryGetInt16(out short value) => Element.TryGetInt16(out value);

    /// <summary>Gets the current JSON number as a <see cref="short"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="short"/>.</exception>
    public short GetInt16() => Element.GetInt16();

    /// <summary>Tries to get the current JSON number as a <see cref="ushort"/> without throwing.</summary>
    public bool TryGetUInt16(out ushort value) => Element.TryGetUInt16(out value);

    /// <summary>Gets the current JSON number as a <see cref="ushort"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="ushort"/>.</exception>
    public ushort GetUInt16() => Element.GetUInt16();

    /// <summary>Tries to get the current JSON number as an <see cref="int"/> without throwing.</summary>
    public bool TryGetInt32(out int value) => Element.TryGetInt32(out value);

    /// <summary>Gets the current JSON number as an <see cref="int"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="int"/>.</exception>
    public int GetInt32() => Element.GetInt32();

    /// <summary>Tries to get the current JSON number as a <see cref="uint"/> without throwing.</summary>
    public bool TryGetUInt32(out uint value) => Element.TryGetUInt32(out value);

    /// <summary>Gets the current JSON number as a <see cref="uint"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="uint"/>.</exception>
    public uint GetUInt32() => Element.GetUInt32();

    /// <summary>Tries to get the current JSON number as a <see cref="long"/> without throwing.</summary>
    public bool TryGetInt64(out long value) => Element.TryGetInt64(out value);

    /// <summary>Gets the current JSON number as a <see cref="long"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="long"/>.</exception>
    public long GetInt64() => Element.GetInt64();

    /// <summary>Tries to get the current JSON number as a <see cref="ulong"/> without throwing.</summary>
    public bool TryGetUInt64(out ulong value) => Element.TryGetUInt64(out value);

    /// <summary>Gets the current JSON number as a <see cref="ulong"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="ulong"/>.</exception>
    public ulong GetUInt64() => Element.GetUInt64();

    /// <summary>Tries to get the current JSON number as a <see cref="double"/> without throwing.</summary>
    public bool TryGetDouble(out double value) => Element.TryGetDouble(out value);

    /// <summary>Gets the current JSON number as a <see cref="double"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="double"/>.</exception>
    public double GetDouble() => Element.GetDouble();

    /// <summary>Tries to get the current JSON number as a <see cref="float"/> without throwing.</summary>
    public bool TryGetSingle(out float value) => Element.TryGetSingle(out value);

    /// <summary>Gets the current JSON number as a <see cref="float"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="float"/>.</exception>
    public float GetSingle() => Element.GetSingle();

    /// <summary>Tries to get the current JSON number as a <see cref="decimal"/> without throwing.</summary>
    public bool TryGetDecimal(out decimal value) => Element.TryGetDecimal(out value);

    /// <summary>Gets the current JSON number as a <see cref="decimal"/>.</summary>
    /// <exception cref="FormatException">Out of range for <see cref="decimal"/>.</exception>
    public decimal GetDecimal() => Element.GetDecimal();

    /// <summary>
    /// Gets the property name for the current property value.
    /// </summary>
    internal string GetPropertyName() => Element.GetPropertyName();

    /// <summary>
    /// Gets the property name (UTF-8 raw span) for the current property value.
    /// </summary>
    internal ReadOnlySpan<byte> GetPropertyNameRaw() => Element.GetPropertyNameRaw();

    /// <summary>
    /// Returns the raw JSON text of the current value.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// The parent <see cref="SourceResultDocument"/> has been disposed.
    /// </exception>
    public string GetRawText()
        => JsonReaderHelper.TranscodeHelper(_parent.GetRawValue(_cursor, _row, includeQuotes: true));

    internal string GetPropertyRawText() => Element.GetPropertyRawText();

    internal ReadOnlySpan<byte> GetRawValue()
        => _parent.GetRawValue(_cursor, _row, includeQuotes: true);

    public ReadOnlyMemory<byte> GetRawValueAsMemory()
        => _parent.GetRawValueAsMemory(_cursor, _row, includeQuotes: true);

    internal ReadOnlySpan<byte> ValueSpan => _parent.GetRawValue(_cursor, _row, includeQuotes: false);

    internal ValueRange GetValuePointer() => Element.GetValuePointer();

    internal SourceResultDocument.DbRow GetValueRow() => _row;

    /// <summary>
    /// Returns this snapshot, so that snapshot-creating code paths can operate on either
    /// a <see cref="SourceResultElement"/> or an existing snapshot.
    /// </summary>
    internal SourceResultElementSnapshot CreateSnapshot() => this;

    /// <summary>
    /// Tries to get the raw UTF-8 bytes of a JSON string value that contains no escape sequences.
    /// </summary>
    /// <param name="utf8Value">Receives the raw UTF-8 span when successful.</param>
    /// <returns>
    /// <see langword="false"/> when the value is not a string, contains escape sequences,
    /// or is not backed by contiguous memory.
    /// </returns>
    internal bool TryGetRawStringValue(out ReadOnlySpan<byte> utf8Value)
        => _parent.TryGetRawStringValue(_row, out utf8Value);

    /// <summary>
    /// Compares the string value of this element to <paramref name="text"/> without allocation.
    /// </summary>
    public bool ValueEquals(string? text)
    {
        if (TokenType == JsonTokenType.Null)
        {
            return text == null;
        }

        return Element.TextEqualsHelper(text.AsSpan(), isPropertyName: false);
    }

    /// <summary>
    /// Compares the UTF-8 string value of this element to <paramref name="utf8Text"/> without allocation.
    /// </summary>
    public bool ValueEquals(ReadOnlySpan<byte> utf8Text)
    {
        if (TokenType == JsonTokenType.Null)
        {
#pragma warning disable CA2265
            return utf8Text[..0] == default;
#pragma warning restore CA2265
        }

        return Element.TextEqualsHelper(utf8Text, isPropertyName: false, shouldUnescape: true);
    }

    /// <summary>
    /// Compares the string value of this element to <paramref name="text"/> without allocation.
    /// </summary>
    public bool ValueEquals(ReadOnlySpan<char> text)
    {
        if (TokenType == JsonTokenType.Null)
        {
#pragma warning disable CA2265
            return text[..0] == default;
#pragma warning restore CA2265
        }

        return Element.TextEqualsHelper(text, isPropertyName: false);
    }

    internal bool TextEqualsHelper(ReadOnlySpan<byte> utf8Text, bool isPropertyName, bool shouldUnescape)
        => Element.TextEqualsHelper(utf8Text, isPropertyName, shouldUnescape);

    internal bool TextEqualsHelper(ReadOnlySpan<char> text, bool isPropertyName)
        => Element.TextEqualsHelper(text, isPropertyName);

    /// <summary>
    /// Returns an enumerator over the elements of the current JSON array.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON array.
    /// </exception>
    public SourceResultElement.ArrayEnumerator EnumerateArray() => Element.EnumerateArray();

    /// <summary>
    /// Returns an enumerator over the properties of the current JSON object.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The current value is not a JSON object.
    /// </exception>
    public SourceResultElement.ObjectEnumerator EnumerateObject() => Element.EnumerateObject();

    /// <inheritdoc />
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
                Debug.Assert(_parent != null);
                return JsonReaderHelper.TranscodeHelper(
                    _parent.GetRawValue(_cursor, _row, includeQuotes: true));

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
}
