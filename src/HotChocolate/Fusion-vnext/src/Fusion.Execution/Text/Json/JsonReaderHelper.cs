using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Text.Json;

internal static class JsonReaderHelper
{
    public static JsonValueKind ToValueKind(this ElementTokenType tokenType)
    {
        switch (tokenType)
        {
            case ElementTokenType.None:
                return JsonValueKind.Undefined;
            case ElementTokenType.StartArray:
                return JsonValueKind.Array;
            case ElementTokenType.StartObject:
                return JsonValueKind.Object;
            case ElementTokenType.String:
            case ElementTokenType.Number:
            case ElementTokenType.True:
            case ElementTokenType.False:
            case ElementTokenType.Null:
                // This is the offset between the set of literals within JsonValueType and JsonTokenType
                // Essentially: JsonTokenType.Null - JsonValueType.Null
                return (JsonValueKind)((byte)tokenType - 4);
            default:
                Debug.Fail($"No mapping for token type {tokenType}");
                return JsonValueKind.Undefined;
        }
    }

    public static bool TryGetValue(ReadOnlySpan<byte> segment, bool isEscaped, out DateTime value)
    {
        if (!JsonHelpers.IsValidDateTimeOffsetParseLength(segment.Length))
        {
            value = default;
            return false;
        }

        // Segment needs to be unescaped
        if (isEscaped)
        {
            return TryGetEscapedDateTime(segment, out value);
        }

        Debug.Assert(segment.IndexOf(JsonConstants.BackSlash) == -1);

        if (JsonHelpers.TryParseAsISO(segment, out DateTime tmp))
        {
            value = tmp;
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetEscapedDateTime(ReadOnlySpan<byte> source, out DateTime value)
    {
        Debug.Assert(source.Length <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
        Span<byte> sourceUnescaped = stackalloc byte[JsonConstants.MaximumEscapedDateTimeOffsetParseLength];

        Unescape(source, sourceUnescaped, out int written);
        Debug.Assert(written > 0);

        sourceUnescaped = sourceUnescaped.Slice(0, written);
        Debug.Assert(!sourceUnescaped.IsEmpty);

        if (JsonHelpers.IsValidUnescapedDateTimeOffsetParseLength(sourceUnescaped.Length)
            && JsonHelpers.TryParseAsISO(sourceUnescaped, out DateTime tmp))
        {
            value = tmp;
            return true;
        }

        value = default;
        return false;
    }

    internal static void Unescape(ReadOnlySpan<byte> source, Span<byte> destination, out int written)
    {
        Debug.Assert(destination.Length >= source.Length);

        int idx = source.IndexOf(JsonConstants.BackSlash);
        Debug.Assert(idx >= 0);

        bool result = TryUnescape(source, destination, idx, out written);
        Debug.Assert(result);
    }

}
