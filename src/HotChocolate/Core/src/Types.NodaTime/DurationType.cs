using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using NodaTime;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// The <c>Duration</c> scalar type represents a duration of time. It is intended for scenarios
/// where you need to represent time intervals, such as elapsed time, timeout durations, scheduling
/// intervals, or any measurement of time that is not tied to a specific date or time.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/duration.html">Specification</seealso>
public class DurationType : ScalarType<Duration, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/duration.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="DurationType"/> class.
    /// </summary>
    public DurationType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        Pattern =
            @"^-?P(?:-?\d+Y)?(?:-?\d+M)?(?:-?\d+W)?(?:-?\d+D)?(?:T(?:-?\d+H)?(?:-?\d+M)?(?:-?\d+(?:[.,]\d+)?S)?)?$";
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DurationType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DurationType()
        : this(
            ScalarNames.Duration,
            TypeResources.DurationType_Description,
            BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    protected override Duration OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        // Parse directly from UTF-8 bytes to avoid the string allocation.
        if (Iso8601DurationParser.TryParse(valueLiteral.AsSpan(), out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override Duration OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (Iso8601DurationParser.TryParse(inputValue.GetString()!.AsSpan(), out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(Duration runtimeValue, ResultElement resultValue)
    {
        // Format directly to UTF-8 bytes on the stack to avoid allocation.
        Span<byte> buffer = stackalloc byte[64];
        Iso8601DurationFormatter.TryFormat(runtimeValue, buffer, out var bytesWritten);
        resultValue.SetStringValue(buffer[..bytesWritten]);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(Duration runtimeValue)
        => new StringValueNode(Iso8601DurationFormatter.Format(runtimeValue));
}
