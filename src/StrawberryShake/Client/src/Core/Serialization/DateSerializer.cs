using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles date scalars.
/// </summary>
public class DateSerializer : ScalarSerializer<string, DateOnly>
{
    private const string DateFormat = "yyyy-MM-dd";

    public DateSerializer(string typeName = BuiltInScalarNames.Date)
        : base(typeName)
    {
    }

    public override DateOnly Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, out var date))
        {
            return date.Value;
        }

        throw ThrowHelper.DateSerializer_InvalidFormat(serializedValue);
    }

    protected override string Format(DateOnly runtimeValue)
    {
        return runtimeValue.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateOnly? value)
    {
        if (DateOnly.TryParseExact(
            serialized,
            DateFormat,
            out var date))
        {
            value = date;
            return true;
        }

        value = null;
        return false;
    }
}
