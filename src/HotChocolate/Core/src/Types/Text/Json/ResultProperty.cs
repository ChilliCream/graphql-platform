using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Text.Json;

/// <summary>
///   Represents a single property for a JSON object.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct ResultProperty
{
    internal ResultProperty(ResultElement value)
    {
        Value = value;
    }

    /// <summary>
    ///   The value of this property.
    /// </summary>
    public ResultElement Value { get; }

    /// <summary>
    ///   The name of this property.
    ///   This allocates a new string instance for each call.
    /// </summary>
    public string Name => Value.GetPropertyName();

    public Selection? Selection => Value.Selection;

    public Selection AssertSelection() => Value.AssertSelection();

    /// <summary>
    ///   Compares <paramref name="text" /> to the name of this property.
    /// </summary>
    /// <param name="text">The text to compare against.</param>
    /// <returns>
    ///   <see langword="true" /> if the name of this property matches <paramref name="text"/>,
    ///   <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="Type"/> is not <see cref="JsonTokenType.PropertyName"/>.
    /// </exception>
    /// <remarks>
    ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
    ///   <see cref="Name" />, but can avoid creating the string instance.
    /// </remarks>
    public bool NameEquals(string? text)
    {
        return NameEquals(text.AsSpan());
    }

    /// <summary>
    ///   Compares the text represented by <paramref name="utf8Text" /> to the name of this property.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to compare against.</param>
    /// <returns>
    ///   <see langword="true" /> if the name of this property has the same UTF-8 encoding as
    ///   <paramref name="utf8Text" />, <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="Type"/> is not <see cref="JsonTokenType.PropertyName"/>.
    /// </exception>
    /// <remarks>
    ///   This method is functionally equal to doing an ordinal comparison of <paramref name="utf8Text" /> and
    ///   <see cref="Name" />, but can avoid creating the string instance.
    /// </remarks>
    public bool NameEquals(ReadOnlySpan<byte> utf8Text)
    {
        return Value.TextEqualsHelper(utf8Text, isPropertyName: true, shouldUnescape: true);
    }

    /// <summary>
    ///   Compares <paramref name="text" /> to the name of this property.
    /// </summary>
    /// <param name="text">The text to compare against.</param>
    /// <returns>
    ///   <see langword="true" /> if the name of this property matches <paramref name="text"/>,
    ///   <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   This value's <see cref="Type"/> is not <see cref="JsonTokenType.PropertyName"/>.
    /// </exception>
    /// <remarks>
    ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
    ///   <see cref="Name" />, but can avoid creating the string instance.
    /// </remarks>
    public bool NameEquals(ReadOnlySpan<char> text)
    {
        return Value.TextEqualsHelper(text, isPropertyName: true);
    }

    internal bool EscapedNameEquals(ReadOnlySpan<byte> utf8Text)
    {
        return Value.TextEqualsHelper(utf8Text, isPropertyName: true, shouldUnescape: false);
    }

    internal ReadOnlySpan<byte> NameSpan => Value.GetPropertyNameRaw();

    /// <summary>
    ///   Provides a <see cref="string"/> representation of the property for
    ///   debugging purposes.
    /// </summary>
    /// <returns>
    ///   A string containing the un-interpreted value of the property, beginning
    ///   at the declaring open-quote and ending at the last character that is part of
    ///   the value.
    /// </returns>
    public override string ToString()
    {
        return Value.GetPropertyRawText();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
        => Value.ValueKind == JsonValueKind.Undefined ? "<Undefined>" : $"\"{ToString()}\"";

    public void Deconstruct(out Selection selection, out ResultElement value)
    {
        selection = AssertSelection();
        value = Value;
    }
}
