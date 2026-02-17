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
/// The <c>DateTime</c> scalar type represents a date and time with time zone offset information. It
/// is intended for scenarios where representing a specific instant in time is required, such as
/// recording when an event occurred, scheduling future events across time zones, or storing
/// timestamps for auditing purposes.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/date-time.html">Specification</seealso>
public partial class DateTimeType : ScalarType<DateTimeOffset, StringValueNode>
{
    private const string UtcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.FFFFFFFZ";
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss.FFFFFFFzzz";
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/date-time.html";

    private readonly bool _enforceSpecFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    public DateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?(?:[Zz]|[+-]\d{2}:\d{2})$";
        SpecifiedBy = new Uri(SpecifiedByUri);
        _enforceSpecFormat = !disableFormatCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    public DateTimeType(bool disableFormatCheck)
        : this(
            ScalarNames.DateTime,
            TypeResources.DateTimeType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DateTimeType()
        : this(
            ScalarNames.DateTime,
            TypeResources.DateTimeType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: false)
    {
    }

    protected override DateTimeOffset OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    protected override DateTimeOffset OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    protected override void OnCoerceOutputValue(DateTimeOffset runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue.Offset == TimeSpan.Zero)
        {
            resultValue.SetStringValue(runtimeValue.ToString(UtcFormat, CultureInfo.InvariantCulture));
        }
        else
        {
            resultValue.SetStringValue(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));
        }
    }

    protected override StringValueNode OnValueToLiteral(DateTimeOffset runtimeValue)
    {
        if (runtimeValue.Offset == TimeSpan.Zero)
        {
            return new StringValueNode(runtimeValue.ToString(UtcFormat, CultureInfo.InvariantCulture));
        }
        else
        {
            return new StringValueNode(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));
        }
    }

    private bool TryParseStringValue(string serialized, out DateTimeOffset value)
    {
        // Check format.
        if (_enforceSpecFormat && !DateTimeRegex().IsMatch(serialized))
        {
            value = default;
            return false;
        }

        if (DateTimeOffset.TryParse(
            serialized,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dt))
        {
            value = dt;
            return true;
        }

        value = default;
        return false;
    }

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,9})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex();
}
