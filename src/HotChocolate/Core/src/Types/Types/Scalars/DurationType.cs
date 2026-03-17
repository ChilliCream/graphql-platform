using System.Text.Json;
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
            DurationFormat.Iso8601 => null,
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
        if (Format == DurationFormat.Iso8601)
        {
            // Parse directly from UTF-8 bytes to avoid the string allocation.
            if (Iso8601DurationParser.TryParse(valueLiteral.AsSpan(), out var value))
            {
                return value;
            }
        }
        else
        {
            if (TimeSpan.TryParse(valueLiteral.Value, out var value))
            {
                return value;
            }
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override TimeSpan OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        var str = inputValue.GetString()!;

        if (Format == DurationFormat.Iso8601)
        {
            if (Iso8601DurationParser.TryParse(str.AsSpan(), out var value))
            {
                return value;
            }
        }
        else
        {
            if (TimeSpan.TryParse(str, out var value))
            {
                return value;
            }
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(TimeSpan runtimeValue, ResultElement resultValue)
    {
        if (Format == DurationFormat.Iso8601)
        {
            // Format directly to UTF-8 bytes on the stack to avoid allocation.
            Span<byte> buffer = stackalloc byte[64];
            Iso8601DurationFormatter.TryFormat(runtimeValue, buffer, out var bytesWritten);
            resultValue.SetStringValue(buffer[..bytesWritten]);
        }
        else
        {
            resultValue.SetStringValue(runtimeValue.ToString("c"));
        }
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TimeSpan runtimeValue)
    {
        return Format == DurationFormat.Iso8601
            ? new StringValueNode(Iso8601DurationFormatter.Format(runtimeValue))
            : new StringValueNode(runtimeValue.ToString("c"));
    }
}
