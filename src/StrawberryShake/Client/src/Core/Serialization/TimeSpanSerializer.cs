using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles time-span scalars.
/// </summary>
public class TimeSpanSerializer : ScalarSerializer<string, TimeSpan>
{
    private readonly TimeSpanFormat _format;

    public TimeSpanSerializer(
        string typeName = BuiltInScalarNames.TimeSpan,
        TimeSpanFormat format = TimeSpanFormat.Iso8601)
        : base(typeName)
    {
        _format = format;
    }

    public override TimeSpan Parse(string serializedValue)
    {
        if (TryDeserializeFromString(serializedValue, _format, out var timeSpan))
        {
            return timeSpan!.Value;
        }

        throw ThrowHelper.TimeSpanSerializer_CouldNotParseValue(serializedValue, _format);
    }

    protected override string Format(TimeSpan runtimeValue)
    {
        if (TrySerialize(runtimeValue, out var serializedValue))
        {
            return serializedValue;
        }

        throw ThrowHelper.TimeSpanSerializer_CouldNotFormatValue(runtimeValue, _format);
    }

    private static bool TryDeserializeFromString(
        string serialized,
        TimeSpanFormat format,
        out TimeSpan? value)
    {
        return format == TimeSpanFormat.Iso8601
            ? TryDeserializeIso8601(serialized, out value)
            : TryDeserializeDotNet(serialized, out value);
    }

    private bool TrySerialize(
        object? runtimeValue,
        [NotNullWhen(true)] out string? resultValue)
    {
        if (runtimeValue is TimeSpan timeSpan)
        {
            if (_format == TimeSpanFormat.Iso8601)
            {
                resultValue = XmlConvert.ToString(timeSpan);
                return true;
            }

            resultValue = timeSpan.ToString("c");
            return true;
        }

        resultValue = null;
        return false;
    }

    private static bool TryDeserializeIso8601(string serialized, out TimeSpan? value)
    {
        try
        {
            return Iso8601Duration.TryParse(serialized, out value);
        }
        catch (FormatException)
        {
            value = null;
            return false;
        }
    }

    private static bool TryDeserializeDotNet(string serialized, out TimeSpan? value)
    {
        if (TimeSpan.TryParse(serialized, out var timeSpan))
        {
            value = timeSpan;
            return true;
        }

        value = null;
        return false;
    }
}
