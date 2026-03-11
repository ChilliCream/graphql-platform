using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles <c>LocalDateTime</c> scalars.
/// </summary>
public class LocalDateTimeSerializer : ScalarSerializer<string, DateTime>
{
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss.FFFFFFF";

    public LocalDateTimeSerializer(string typeName = BuiltInScalarNames.LocalDateTime)
        : base(typeName)
    {
    }

    public override DateTime Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, out var dateTime))
        {
            return dateTime.Value;
        }

        throw ThrowHelper.LocalDateTimeSerializer_InvalidFormat(serializedValue);
    }

    protected override string Format(DateTime runtimeValue)
    {
        return runtimeValue.ToString(
            LocalFormat,
            CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateTime? value)
    {
        if (serialized is not null
            && DateTime.TryParse(
                serialized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            value = dt;
            return true;
        }

        value = null;
        return false;
    }
}
