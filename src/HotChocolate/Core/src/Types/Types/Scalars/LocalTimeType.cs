using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The <c>LocalTime</c> scalar type represents a time of day without date or time zone information.
/// It is intended for scenarios where only the time component matters, such as business operating
/// hours (e.g., "opens at 09:00"), daily schedules, or recurring time-based events where the
/// specific date is irrelevant.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/local-time.html">Specification</seealso>
public partial class LocalTimeType : ScalarType<TimeOnly, StringValueNode>
{
    private const string LocalFormat = "HH:mm:ss.FFFFFFF";
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/local-time.html";

    private readonly bool _enforceSpecFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    public LocalTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?$";
        SpecifiedBy = new Uri(SpecifiedByUri);
        _enforceSpecFormat = !disableFormatCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    public LocalTimeType(bool disableFormatCheck)
        : this(
            ScalarNames.LocalTime,
            TypeResources.LocalTimeType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalTimeType()
        : this(
            ScalarNames.LocalTime,
            TypeResources.LocalTimeType_Description)
    {
    }

    /// <inheritdoc />
    protected override TimeOnly OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override TimeOnly OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(TimeOnly runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TimeOnly runtimeValue)
        => new StringValueNode(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out TimeOnly value)
    {
        // Check format.
        if (_enforceSpecFormat && !LocalTimeRegex().IsMatch(serialized))
        {
            value = default;
            return false;
        }

        if (TimeOnly.TryParse(
            serialized,
            CultureInfo.InvariantCulture,
            out var time))
        {
            value = time;
            return true;
        }

        value = default;
        return false;
    }

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,9})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex();
}
