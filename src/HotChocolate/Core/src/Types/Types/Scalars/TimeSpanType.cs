using System.Xml;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The TimeSpan scalar type represented in two formats:
/// <see cref="TimeSpanFormat.Iso8601"/> and <see cref="TimeSpanFormat.DotNet"/>
/// </summary>
public class TimeSpanType
    : ScalarType<TimeSpan, StringValueNode>
{
    private readonly TimeSpanFormat _format;

    public TimeSpanType(
        TimeSpanFormat format = TimeSpanFormat.Iso8601,
        BindingBehavior bind = BindingBehavior.Implicit)
        : this(ScalarNames.TimeSpan, TypeResources.TimeSpanType_Description, format, bind)
    {
    }

    public TimeSpanType(
        string name,
        string? description = default,
        TimeSpanFormat format = TimeSpanFormat.Iso8601,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        _format = format;
        Description = description;
    }

    [ActivatorUtilitiesConstructor]
    public TimeSpanType()
        : this(ScalarNames.TimeSpan, TypeResources.TimeSpanType_Description)
    {
    }

    protected override TimeSpan ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, _format, out var value) &&
            value != null)
        {
            return value.Value;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(TimeSpan runtimeValue)
    {
        return _format == TimeSpanFormat.Iso8601
            ? new StringValueNode(XmlConvert.ToString(runtimeValue))
            : new StringValueNode(runtimeValue.ToString("c"));
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s &&
            TryDeserializeFromString(s, _format, out var timeSpan))
        {
            return ParseValue(timeSpan);
        }

        if (resultValue is TimeSpan ts)
        {
            return ParseValue(ts);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
            this);
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

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

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is string s &&
            TryDeserializeFromString(s, _format, out var timeSpan))
        {
            runtimeValue = timeSpan;
            return true;
        }

        if (resultValue is TimeSpan ts)
        {
            runtimeValue = ts;
            return true;
        }

        runtimeValue = null;
        return false;
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
