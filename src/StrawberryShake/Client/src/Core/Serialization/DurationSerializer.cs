using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles <c>Duration</c> scalars.
/// </summary>
public class DurationSerializer : ScalarSerializer<string, TimeSpan>
{
    private readonly DurationFormat _format;

    public DurationSerializer(
        string typeName = BuiltInScalarNames.Duration,
        DurationFormat format = DurationFormat.Iso8601)
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

        throw ThrowHelper.DurationSerializer_CouldNotParseValue(serializedValue, _format);
    }

    protected override string Format(TimeSpan runtimeValue)
    {
        if (TrySerialize(runtimeValue, out var serializedValue))
        {
            return serializedValue;
        }

        throw ThrowHelper.DurationSerializer_CouldNotFormatValue(runtimeValue, _format);
    }

    private static bool TryDeserializeFromString(
        string serialized,
        DurationFormat format,
        out TimeSpan? value)
    {
        return format == DurationFormat.Iso8601
            ? TryDeserializeIso8601(serialized, out value)
            : TryDeserializeDotNet(serialized, out value);
    }

    private bool TrySerialize(
        object? runtimeValue,
        [NotNullWhen(true)] out string? resultValue)
    {
        if (runtimeValue is TimeSpan timeSpan)
        {
            if (_format == DurationFormat.Iso8601)
            {
                resultValue = Iso8601DurationFormatter.Format(timeSpan);
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
        if (Iso8601DurationParser.TryParse(serialized.AsSpan(), out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
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
