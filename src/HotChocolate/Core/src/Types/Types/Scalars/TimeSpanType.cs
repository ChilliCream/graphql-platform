using System.Text.Json;
using System.Xml;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The TimeSpan scalar type represented in two formats:
/// <see cref="TimeSpanFormat.Iso8601"/> and <see cref="TimeSpanFormat.DotNet"/>
/// </summary>
public class TimeSpanType : ScalarType<TimeSpan, StringValueNode>
{
    public TimeSpanFormat Format { get; }

    public TimeSpanType(
        TimeSpanFormat format = TimeSpanFormat.Iso8601,
        BindingBehavior bind = BindingBehavior.Implicit)
        : this(ScalarNames.TimeSpan, TypeResources.TimeSpanType_Description, format, bind)
    {
    }

    public TimeSpanType(
        string name,
        string? description = null,
        TimeSpanFormat format = TimeSpanFormat.Iso8601,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Format = format;
        Description = description;
        Pattern = format switch
        {
            TimeSpanFormat.Iso8601
                => @"^-?P(?:\d+W|(?=\d|T(?:\d|$))(?:\d+Y)?(?:\d+M)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$",
            TimeSpanFormat.DotNet
                => @"^-?(?:(?:\d{1,8})\.)?(?:[0-1]?\d|2[0-3]):(?:[0-5]?\d):(?:[0-5]?\d)(?:\.(?:\d{1,7}))?$",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    [ActivatorUtilitiesConstructor]
    public TimeSpanType()
        : this(ScalarNames.TimeSpan, TypeResources.TimeSpanType_Description)
    {
    }

    /// <inheritdoc />
    protected override TimeSpan OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, Format, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override TimeSpan OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, Format, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(TimeSpan runtimeValue, ResultElement resultValue)
    {
        var serialized = Format == TimeSpanFormat.Iso8601
            ? XmlConvert.ToString(runtimeValue)
            : runtimeValue.ToString("c");
        resultValue.SetStringValue(serialized);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TimeSpan runtimeValue)
    {
        return Format == TimeSpanFormat.Iso8601
            ? new StringValueNode(XmlConvert.ToString(runtimeValue))
            : new StringValueNode(runtimeValue.ToString("c"));
    }

    private static bool TryParseStringValue(
        string serialized,
        TimeSpanFormat format,
        out TimeSpan value)
    {
        return format == TimeSpanFormat.Iso8601
            ? TryParseIso8601(serialized, out value)
            : TryParseDotNet(serialized, out value);
    }

    private static bool TryParseIso8601(string serialized, out TimeSpan value)
    {
        try
        {
            if (Iso8601Duration.TryParse(serialized, out var nullable) && nullable.HasValue)
            {
                value = nullable.Value;
                return true;
            }
        }
        catch (FormatException)
        {
        }

        value = default;
        return false;
    }

    private static bool TryParseDotNet(string serialized, out TimeSpan value)
    {
        if (TimeSpan.TryParse(serialized, out var timeSpan))
        {
            value = timeSpan;
            return true;
        }

        value = default;
        return false;
    }
}
