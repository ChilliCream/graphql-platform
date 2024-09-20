using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles date scalars.
/// </summary>
public class DateSerializer : ScalarSerializer<string, DateTime>
{
    private const string _dateFormat = "yyyy-MM-dd";

    public DateSerializer(string typeName = BuiltInScalarNames.Date)
        : base(typeName)
    {
    }

    public override DateTime Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, out var date))
        {
            return date.Value;
        }

        throw ThrowHelper.DateTimeSerializer_InvalidFormat(serializedValue);
    }

    protected override string Format(DateTime runtimeValue)
    {
        return runtimeValue.Date.ToString(_dateFormat, CultureInfo.InvariantCulture);
    }

    private static bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out DateTime? value)
    {
        if (DateTime.TryParse(
            serialized,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out var dateTime))
        {
            value = dateTime.Date;
            return true;
        }

        value = null;
        return false;
    }
}
