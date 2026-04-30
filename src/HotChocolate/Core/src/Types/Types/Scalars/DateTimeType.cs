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

    private readonly DateTimeOptions _options;
    private readonly string _utcFormat;
    private readonly string _localFormat;
    private readonly Regex _dateTimeRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    public DateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        DateTimeOptions? options = null)
        : base(name, bind)
    {
        _options = options ?? new DateTimeOptions();
        Description = description;
        Pattern = GetPattern();
        SpecifiedBy = new Uri(SpecifiedByUri);
        _utcFormat = GetUtcFormat();
        _localFormat = GetLocalFormat();
        _dateTimeRegex = GetDateTimeRegex();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    public DateTimeType(DateTimeOptions options)
        : this(
            ScalarNames.DateTime,
            TypeResources.DateTimeType_Description,
            BindingBehavior.Implicit,
            options: options)
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
            options: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts DateTimeOptions instead.")]
    public DateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false)
        : base(name, bind)
    {
        _options = new DateTimeOptions { ValidateInputFormat = !disableFormatCheck };
        Description = description;
        Pattern = GetPattern();
        SpecifiedBy = new Uri(SpecifiedByUri);
        _utcFormat = GetUtcFormat();
        _localFormat = GetLocalFormat();
        _dateTimeRegex = GetDateTimeRegex();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeType"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts DateTimeOptions instead.")]
    public DateTimeType(bool disableFormatCheck)
        : this(
            ScalarNames.DateTime,
            TypeResources.DateTimeType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
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
        resultValue.SetStringValue(
            runtimeValue.ToString(
                runtimeValue.Offset == TimeSpan.Zero ? _utcFormat : _localFormat,
                CultureInfo.InvariantCulture));
    }

    protected override StringValueNode OnValueToLiteral(DateTimeOffset runtimeValue)
    {
        return new StringValueNode(
            runtimeValue.ToString(
                runtimeValue.Offset == TimeSpan.Zero ? _utcFormat : _localFormat,
                CultureInfo.InvariantCulture));
    }

    private bool TryParseStringValue(string serialized, out DateTimeOffset value)
    {
        // Check format.
        if (_options.ValidateInputFormat)
        {
            var match = _dateTimeRegex.Match(serialized);

            // RFC 3339 does not support specifying the hour as 24.
            if (!match.Success || match.Groups["hour"].Value == "24")
            {
                value = default;
                return false;
            }
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

    private string GetPattern()
        => _options.InputPrecision == 0
            ? @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:[Zz]|[+-]\d{2}:\d{2})$"
            : @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,"
                + _options.InputPrecision
                + @"})?(?:[Zz]|[+-]\d{2}:\d{2})$";

    private string GetUtcFormat()
        => _options.OutputPrecision switch
        {
            DateTimeOptions.DefaultOutputPrecision => UtcFormat,
            0 => @"yyyy-MM-ddTHH\:mm\:ssZ",
            _ => @$"yyyy-MM-ddTHH\:mm\:ss.{new string('F', _options.OutputPrecision)}Z"
        };

    private string GetLocalFormat()
        => _options.OutputPrecision switch
        {
            DateTimeOptions.DefaultOutputPrecision => LocalFormat,
            0 => @"yyyy-MM-ddTHH\:mm\:sszzz",
            _ => @$"yyyy-MM-ddTHH\:mm\:ss.{new string('F', _options.OutputPrecision)}zzz"
        };

    private Regex GetDateTimeRegex()
        => _options.InputPrecision switch
        {
            0 => DateTimeRegex0(),
            1 => DateTimeRegex1(),
            2 => DateTimeRegex2(),
            3 => DateTimeRegex3(),
            4 => DateTimeRegex4(),
            5 => DateTimeRegex5(),
            6 => DateTimeRegex6(),
            7 => DateTimeRegex7(),
            8 => DateTimeRegex8(),
            _ => DateTimeRegex9()
        };

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex0();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9])?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex1();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,2})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex2();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,3})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex3();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,4})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex4();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,5})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex5();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,6})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex6();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex7();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,8})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex8();

    [GeneratedRegex(
        @"^[0-9]{4}-[0-9]{2}-[0-9]{2}T(?<hour>[0-9]{2}):[0-9]{2}:[0-9]{2}(\.[0-9]{1,9})?(Z|[+-][0-9]{2}:[0-9]{2})\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex DateTimeRegex9();
}
