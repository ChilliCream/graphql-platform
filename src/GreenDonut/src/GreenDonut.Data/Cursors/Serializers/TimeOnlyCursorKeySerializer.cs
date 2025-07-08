using System.Buffers;
using System.Reflection;
using System.Text.Unicode;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class TimeOnlyCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo s_compareTo =
        CompareToResolver.GetCompareToMethod<TimeOnly>();

    private const string TimeFormat = "HHmmssfffffff";

    public bool IsSupported(Type type)
        => type == typeof(TimeOnly) || type == typeof(TimeOnly?);

    public MethodInfo GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        Span<char> timeOnlyChars = stackalloc char[TimeFormat.Length];

        if (Utf8.ToUtf16(formattedKey, timeOnlyChars, out _, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid TimeOnly.");
        }

        // Parse date.
        return TimeOnly.ParseExact(timeOnlyChars, TimeFormat);
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var timeOnly = (TimeOnly)key;
        Span<char> characters = stackalloc char[TimeFormat.Length];

        // Format time.
        if (!timeOnly.TryFormat(characters, out _, TimeFormat))
        {
            written = 0;
            return false;
        }

        return Utf8.FromUtf16(characters, buffer, out _, out written) == OperationStatus.Done;
    }
}
