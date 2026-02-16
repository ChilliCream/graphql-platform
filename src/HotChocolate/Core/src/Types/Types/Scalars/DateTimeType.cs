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
/// <para>
/// This scalar represents an exact point in time. This point in time is specified by having an
/// offset to UTC and does <b>not</b> use a time zone.
/// </para>
/// <para>
/// It is a slightly refined version of
/// <see href="https://tools.ietf.org/html/rfc3339">RFC 3339</see>, including the
/// <see href="https://www.rfc-editor.org/errata/rfc3339">errata</see>.
/// </para>
/// </summary>
/// <seealso href="https://scalars.graphql.org/andimarek/date-time.html">Specification</seealso>
public class DateTimeType : ScalarType<DateTimeOffset, StringValueNode>
{
    private const string UtcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";
    private const string SpecifiedByUri = "https://scalars.graphql.org/andimarek/date-time.html";

    private static readonly Regex s_dateTimeScalarRegex = new(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|[+-][0-9]{2}:[0-9]{2})$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

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
        Pattern = @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:[Zz]|[+-]\d{2}:\d{2})$";
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

    private bool TryParseStringValue(string serialized, out DateTimeOffset runtimeValue)
    {
        // Check format.
        if (_enforceSpecFormat && !s_dateTimeScalarRegex.IsMatch(serialized))
        {
            runtimeValue = default;
            return false;
        }

        // No "Unknown Local Offset Convention".
        // https://scalars.graphql.org/andimarek/date-time.html#sec-Overview.No-Unknown-Local-Offset-Convention-
        if (serialized.EndsWith("-00:00"))
        {
            runtimeValue = default;
            return false;
        }

        if (DateTimeOffset.TryParse(serialized, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            runtimeValue = dt;
            return true;
        }

        runtimeValue = default;
        return false;
    }
}
