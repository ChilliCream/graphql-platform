using System.Buffers;
using System.Reflection;
using System.Text.Unicode;

namespace HotChocolate.Pagination.Serialization;

internal sealed class TimeOnlyCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo _compareTo =
        CompareToResolver.GetCompareToMethod<TimeOnly>();

    private const string _timeFormat = "HHmmssfffffff";

    public bool IsSupported(Type type)
        => type == typeof(TimeOnly);

    public MethodInfo GetCompareToMethod(Type type)
        => _compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        Span<char> timeOnlyChars = stackalloc char[_timeFormat.Length];

        if (Utf8.ToUtf16(formattedKey, timeOnlyChars, out _, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid TimeOnly.");
        }

        // Parse date.
        return TimeOnly.ParseExact(timeOnlyChars, _timeFormat);
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var timeOnly = (TimeOnly)key;
        Span<char> characters = stackalloc char[_timeFormat.Length];

        // Format time.
        if (!timeOnly.TryFormat(characters, out _, _timeFormat))
        {
            written = 0;
            return false;
        }

        return Utf8.FromUtf16(characters, buffer, out _, out written) == OperationStatus.Done;
    }
}
