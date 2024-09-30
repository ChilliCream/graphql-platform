using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles date-time scalars.
/// </summary>
public class DateTimeSerializer : ScalarSerializer<string, DateTimeOffset>
{
    private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
    private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

    public DateTimeSerializer(string typeName = BuiltInScalarNames.DateTime)
        : base(typeName)
    {
    }

    public override DateTimeOffset Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, out var dateTimeOffset))
        {
            return dateTimeOffset.Value;
        }

        throw ThrowHelper.DateTimeSerializer_InvalidFormat(serializedValue);
    }

    protected override string Format(DateTimeOffset runtimeValue)
    {
        if (runtimeValue.Offset == TimeSpan.Zero)
        {
            return runtimeValue.ToString(
                _utcFormat,
                CultureInfo.InvariantCulture);
        }

        return runtimeValue.ToString(
            _localFormat,
            CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateTimeOffset? value)
    {
        if (serialized is not null
            && DateTimeOffset.TryParse(
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
