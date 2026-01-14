using System.Buffers;
using System.Globalization;
using System.Reflection;
using System.Text.Unicode;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class DateTimeCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo s_compareTo =
        CompareToResolver.GetCompareToMethod<DateTime>();

    private const string DateTimeFormat = "yyyyMMddHHmmssfffffff";

    public bool IsSupported(Type type)
        => type == typeof(DateTime);

    public MethodInfo GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        var dateTimeBytes = formattedKey[..DateTimeFormat.Length];
        Span<char> dateTimeChars = stackalloc char[DateTimeFormat.Length];

        if (Utf8.ToUtf16(dateTimeBytes, dateTimeChars, out var read, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid DateTime.");
        }

        // Parse kind.
        var kind = (DateTimeKind)(formattedKey[++read] - '0'); // ++ to skip '#'

        // Parse date and time.
        return DateTime.ParseExact(
            dateTimeChars,
            DateTimeFormat,
            null,
            kind switch
            {
                DateTimeKind.Unspecified
                    => DateTimeStyles.None,
                DateTimeKind.Utc
                    => DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                DateTimeKind.Local
                    => DateTimeStyles.AssumeLocal,
                _ => throw new InvalidOperationException()
            });
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var dateTime = (DateTime)key;
        Span<char> characters = stackalloc char[DateTimeFormat.Length + 2]; // 2 = '#' + 0/1/2

        // Format date and time.
        if (!dateTime.TryFormat(characters, out var charsWritten, DateTimeFormat))
        {
            written = 0;
            return false;
        }

        // Format kind.
        characters[charsWritten++] = '#';
        characters[charsWritten] = (char)((int)dateTime.Kind + '0');

        return Utf8.FromUtf16(characters, buffer, out _, out written) == OperationStatus.Done;
    }
}
