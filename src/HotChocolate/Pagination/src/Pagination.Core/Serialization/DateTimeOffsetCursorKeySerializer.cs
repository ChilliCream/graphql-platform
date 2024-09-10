using System.Buffers;
using System.Globalization;
using System.Reflection;
using System.Text.Unicode;

namespace HotChocolate.Pagination.Serialization;

internal sealed class DateTimeOffsetCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo _compareTo =
        CompareToResolver.GetCompareToMethod<DateTimeOffset>();

    private const string _dateTimeFormat = "yyyyMMddHHmmssfffffff";
    private const string _offsetFormat = "hhmm";

    public bool IsSupported(Type type)
        => type == typeof(DateTimeOffset);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        var dateTimeBytes = formattedKey[.._dateTimeFormat.Length];
        Span<char> dateTimeChars = stackalloc char[_dateTimeFormat.Length];

        if (Utf8.ToUtf16(dateTimeBytes, dateTimeChars, out var read, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid DateTimeOffset.");
        }

        // Parse date and time.
        var dateTime = DateTime.ParseExact(dateTimeChars, _dateTimeFormat, null);

        // Parse offset sign (- or +).
        var offsetSign = formattedKey[read++];

        var offsetBytes = formattedKey[read..];
        Span<char> offsetChars = stackalloc char[_offsetFormat.Length];

        if (Utf8.ToUtf16(offsetBytes, offsetChars, out _, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid DateTimeOffset.");
        }

        // Parse offset.
        var offset = TimeSpan.ParseExact(
            offsetChars,
            _offsetFormat,
            null,
            offsetSign == '-' ? TimeSpanStyles.AssumeNegative : TimeSpanStyles.None);

        return new DateTimeOffset(dateTime, offset);
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var dateTimeOffset = (DateTimeOffset)key;
        Span<char> characters = stackalloc char[_dateTimeFormat.Length + 1 + _offsetFormat.Length];

        // Format date and time.
        if (!dateTimeOffset.TryFormat(characters, out var charsWritten, _dateTimeFormat))
        {
            written = 0;
            return false;
        }

        // Format offset sign (- or +).
        characters[charsWritten++] = dateTimeOffset.Offset < TimeSpan.Zero ? '-' : '+';

        // Format offset.
        if (!dateTimeOffset.Offset.TryFormat(characters[charsWritten..], out _, _offsetFormat))
        {
            written = 0;
            return false;
        }

        return Utf8.FromUtf16(characters, buffer, out _, out written) == OperationStatus.Done;
    }
}
