using System.Buffers;
using System.Globalization;
using System.Text.Unicode;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class DateTimeOffsetCursorKeySerializer : ICursorKeySerializer
{
    private static readonly CursorKeyCompareMethod s_compareTo =
        CompareToResolver.GetCompareToMethod<DateTimeOffset>();

    private const string DateTimeFormat = "yyyyMMddHHmmssfffffff";
    private const string OffsetFormat = "hhmm";

    public bool IsSupported(Type type)
        => type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);

    public bool IsNullable(Type type)
        => type == typeof(DateTimeOffset?);

    public CursorKeyCompareMethod GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        var dateTimeBytes = formattedKey[..DateTimeFormat.Length];
        Span<char> dateTimeChars = stackalloc char[DateTimeFormat.Length];

        if (Utf8.ToUtf16(dateTimeBytes, dateTimeChars, out var read, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid DateTimeOffset.");
        }

        // Parse date and time.
        var dateTime = DateTime.ParseExact(dateTimeChars, DateTimeFormat, null);

        // Parse offset sign (- or +).
        var offsetSign = formattedKey[read++];

        var offsetBytes = formattedKey[read..];
        Span<char> offsetChars = stackalloc char[OffsetFormat.Length];

        if (Utf8.ToUtf16(offsetBytes, offsetChars, out _, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid DateTimeOffset.");
        }

        // Parse offset.
        var offset = TimeSpan.ParseExact(
            offsetChars,
            OffsetFormat,
            null,
            offsetSign == '-' ? TimeSpanStyles.AssumeNegative : TimeSpanStyles.None);

        return new DateTimeOffset(dateTime, offset);
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var dateTimeOffset = (DateTimeOffset)key;
        Span<char> characters = stackalloc char[DateTimeFormat.Length + 1 + OffsetFormat.Length];

        // Format date and time.
        if (!dateTimeOffset.TryFormat(characters, out var charsWritten, DateTimeFormat))
        {
            written = 0;
            return false;
        }

        // Format offset sign (- or +).
        characters[charsWritten++] = dateTimeOffset.Offset < TimeSpan.Zero ? '-' : '+';

        // Format offset.
        if (!dateTimeOffset.Offset.TryFormat(characters[charsWritten..], out _, OffsetFormat))
        {
            written = 0;
            return false;
        }

        return Utf8.FromUtf16(characters, buffer, out _, out written) == OperationStatus.Done;
    }
}
