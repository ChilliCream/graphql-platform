using System.Buffers;
using System.Reflection;
using System.Text.Unicode;

namespace GreenDonut.Data.Cursors.Serializers;

internal sealed class DateOnlyCursorKeySerializer : ICursorKeySerializer
{
    private static readonly MethodInfo s_compareTo =
        CompareToResolver.GetCompareToMethod<DateOnly>();

    private const string DateFormat = "yyyyMMdd";

    public bool IsSupported(Type type)
        => type == typeof(DateOnly) || type == typeof(DateOnly?);

    public MethodInfo GetCompareToMethod(Type type)
        => s_compareTo;

    public object Parse(ReadOnlySpan<byte> formattedKey)
    {
        Span<char> dateOnlyChars = stackalloc char[DateFormat.Length];

        if (Utf8.ToUtf16(formattedKey, dateOnlyChars, out _, out _) != OperationStatus.Done)
        {
            throw new FormatException("The cursor value is not a valid DateOnly.");
        }

        // Parse date.
        return DateOnly.ParseExact(dateOnlyChars, DateFormat);
    }

    public bool TryFormat(object key, Span<byte> buffer, out int written)
    {
        var dateOnly = (DateOnly)key;
        Span<char> characters = stackalloc char[DateFormat.Length];

        // Format date.
        if (!dateOnly.TryFormat(characters, out _, DateFormat))
        {
            written = 0;
            return false;
        }

        return Utf8.FromUtf16(characters, buffer, out _, out written) == OperationStatus.Done;
    }
}
