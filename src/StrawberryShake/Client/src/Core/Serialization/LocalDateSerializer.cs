using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles <c>LocalDate</c> scalars.
/// </summary>
public class LocalDateSerializer : ScalarSerializer<string, DateOnly>
{
    private const string LocalFormat = "yyyy-MM-dd";

    public LocalDateSerializer(string typeName = BuiltInScalarNames.LocalDate)
        : base(typeName)
    {
    }

    public override DateOnly Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, out var date))
        {
            return date.Value;
        }

        throw ThrowHelper.LocalDateSerializer_InvalidFormat(serializedValue);
    }

    protected override string Format(DateOnly runtimeValue)
    {
        return runtimeValue.ToString(
            LocalFormat,
            CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateOnly? value)
    {
        if (serialized is not null
            && DateOnly.TryParseExact(
                serialized,
                LocalFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            value = date;
            return true;
        }

        value = null;
        return false;
    }
}
