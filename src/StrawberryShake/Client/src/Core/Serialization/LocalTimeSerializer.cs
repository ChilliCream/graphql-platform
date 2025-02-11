using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles <c>LocalTime</c> scalars.
/// </summary>
public class LocalTimeSerializer : ScalarSerializer<string, TimeOnly>
{
    private const string _localFormat = "HH:mm:ss";

    public LocalTimeSerializer(string typeName = BuiltInScalarNames.LocalTime)
        : base(typeName)
    {
    }

    public override TimeOnly Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, out var time))
        {
            return time.Value;
        }

        throw ThrowHelper.LocalTimeSerializer_InvalidFormat(serializedValue);
    }

    protected override string Format(TimeOnly runtimeValue)
    {
        return runtimeValue.ToString(
            _localFormat,
            CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out TimeOnly? value)
    {
        if (serialized is not null
            && TimeOnly.TryParseExact(
                serialized,
                _localFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
        {
            value = time;
            return true;
        }

        value = null;
        return false;
    }
}
