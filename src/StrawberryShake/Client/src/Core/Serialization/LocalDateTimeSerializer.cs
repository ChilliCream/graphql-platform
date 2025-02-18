using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles <c>LocalDateTime</c> scalars.
/// </summary>
public class LocalDateTimeSerializer : ScalarSerializer<string, DateTime>
{
    private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss";

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
            _localFormat,
            CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateTime? value)
    {
        if (serialized is not null
            && DateTime.TryParseExact(
                serialized,
                _localFormat,
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
