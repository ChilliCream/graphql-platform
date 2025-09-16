using System.Diagnostics;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    internal string? GetString(int index, ElementTokenType expectedType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(index);

        var tokenType = row.TokenType;

        if (tokenType == ElementTokenType.Null)
        {
            return null;
        }

        CheckExpectedType(expectedType, tokenType);

        var segment = ReadRawValue(row);

        return row.HasComplexChildren
            ? JsonReaderHelper.GetUnescapedString(segment)
            : JsonReaderHelper.TranscodeHelper(segment);
    }

    internal string GetRequiredString(int index, ElementTokenType expectedType)
    {
        var value = GetString(index, expectedType);

        if (value is null)
        {
            throw new InvalidOperationException("The element value is null.");
        }

        return value;
    }

    internal string GetNameOfPropertyValue(int index)
    {
        // The property name is one row before the property value
        return GetString(index - 1, ElementTokenType.PropertyName)!;
    }

    internal ReadOnlySpan<byte> GetPropertyNameRaw(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(index - 1);
        Debug.Assert(row.TokenType is ElementTokenType.PropertyName);

        return ReadRawValue(row);
    }
}
