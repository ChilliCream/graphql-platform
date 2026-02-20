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
/// The <c>LocalDateTime</c> scalar type represents a date and time without time zone information.
/// It is intended for scenarios where time zone context is either unnecessary or managed
/// separately, such as recording birthdates and times (where the event occurred in a specific local
/// context), displaying timestamps in a user's local time zone (where the time zone is known from
/// context), or recording historical timestamps where the time zone was not captured.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/local-date-time.html">Specification</seealso>
public partial class LocalDateTimeType : ScalarType<DateTime, StringValueNode>
{
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss.FFFFFFF";
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/local-date-time.html";

    private readonly bool _enforceSpecFormat;
    private readonly DateTimeOptions _options;
    private readonly string _localFormat;
    private readonly Regex _localDateTimeRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    public LocalDateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false,
        DateTimeOptions? options = null)
        : base(name, bind)
    {
        options ??= new DateTimeOptions();
        _options = options.Value;
        Description = description;
        Pattern = GetPattern();
        SpecifiedBy = new Uri(SpecifiedByUri);
        _enforceSpecFormat = !disableFormatCheck;
        _localFormat = GetLocalFormat();
        _localDateTimeRegex = GetLocalDateTimeRegex();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    public LocalDateTimeType(bool disableFormatCheck)
        : this(
            ScalarNames.LocalDateTime,
            TypeResources.LocalDateTimeType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    public LocalDateTimeType(DateTimeOptions options)
        : this(
            ScalarNames.LocalDateTime,
            TypeResources.LocalDateTimeType_Description,
            BindingBehavior.Implicit,
            options: options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateTimeType()
        : this(
            ScalarNames.LocalDateTime,
            TypeResources.LocalDateTimeType_Description)
    {
    }

    /// <inheritdoc />
    protected override DateTime OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override DateTime OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(DateTime runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(_localFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(DateTime runtimeValue)
        => new StringValueNode(runtimeValue.ToString(_localFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out DateTime value)
    {
        // Check format.
        if (_enforceSpecFormat && !_localDateTimeRegex.IsMatch(serialized))
        {
            value = default;
            return false;
        }

        if (DateTime.TryParse(
            serialized,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTime))
        {
            value = dateTime;
            return true;
        }

        value = default;
        return false;
    }

    private string GetPattern()
        => _options.InputPrecision == 0
            ? @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}$"
            : @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1," + _options.InputPrecision + "})?$";

    private string GetLocalFormat()
        => _options.OutputPrecision switch
        {
            DateTimeOptions.DefaultOutputPrecision => LocalFormat,
            0 => @"yyyy-MM-ddTHH\:mm\:ss",
            _ => @$"yyyy-MM-ddTHH\:mm\:ss.{new string('F', _options.OutputPrecision)}"
        };

    private Regex GetLocalDateTimeRegex()
        => _options.InputPrecision switch
        {
            0 => LocalDateTimeRegex0(),
            1 => LocalDateTimeRegex1(),
            2 => LocalDateTimeRegex2(),
            3 => LocalDateTimeRegex3(),
            4 => LocalDateTimeRegex4(),
            5 => LocalDateTimeRegex5(),
            6 => LocalDateTimeRegex6(),
            _ => LocalDateTimeRegex7()
        };

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex0();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9])?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex1();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,2})?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex2();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,3})?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex3();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,4})?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex4();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,5})?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex5();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,6})?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex6();

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?\z",
        RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)]
    private static partial Regex LocalDateTimeRegex7();
}
