using System.Text.Json;
using System.Xml;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The <c>Duration</c> scalar type represents a duration of time. It is intended for scenarios
/// where you need to represent time intervals, such as elapsed time, timeout durations, scheduling
/// intervals, or any measurement of time that is not tied to a specific date or time.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/duration.html">Specification</seealso>
public class DurationType : ScalarType<TimeSpan, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/duration.html";

    public DurationFormat Format { get; }

    public DurationType(
        DurationFormat format = DurationFormat.Iso8601,
        BindingBehavior bind = BindingBehavior.Implicit)
        : this(ScalarNames.Duration, TypeResources.DurationType_Description, format, bind)
    {
    }

    public DurationType(
        string name,
        string? description = null,
        DurationFormat format = DurationFormat.Iso8601,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Format = format;
        Description = description;
        Pattern = format switch
        {
            DurationFormat.Iso8601
                => @"^-?P(?:\d+W|(?=\d|T(?:\d|$))(?:\d+Y)?(?:\d+M)?(?:\d+D)?(?:T(?:\d+H)?(?:\d+M)?(?:\d+(?:\.\d+)?S)?)?)$",
            DurationFormat.DotNet
                => @"^-?(?:(?:\d{1,8})\.)?(?:[0-1]?\d|2[0-3]):(?:[0-5]?\d):(?:[0-5]?\d)(?:\.(?:\d{1,7}))?$",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        if (format == DurationFormat.Iso8601)
        {
            SpecifiedBy = new Uri(SpecifiedByUri);
        }
    }

    [ActivatorUtilitiesConstructor]
    public DurationType()
        : this(ScalarNames.Duration, TypeResources.DurationType_Description)
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
        var serialized = Format == DurationFormat.Iso8601
            ? XmlConvert.ToString(runtimeValue)
            : runtimeValue.ToString("c");
        resultValue.SetStringValue(serialized);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TimeSpan runtimeValue)
    {
        return Format == DurationFormat.Iso8601
            ? new StringValueNode(XmlConvert.ToString(runtimeValue))
            : new StringValueNode(runtimeValue.ToString("c"));
    }

    private static bool TryParseStringValue(
        string serialized,
        DurationFormat format,
        out TimeSpan value)
    {
        return format == DurationFormat.Iso8601
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
