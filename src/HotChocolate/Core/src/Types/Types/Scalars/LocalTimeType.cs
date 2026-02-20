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
    private readonly DateTimeOptions _options;
    private readonly string _localFormat;
    private readonly Regex _localTimeRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    public LocalTimeType(
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
        _localTimeRegex = GetLocalTimeRegex();
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
    public LocalTimeType(DateTimeOptions options)
        : this(
            ScalarNames.LocalTime,
            TypeResources.LocalTimeType_Description,
            BindingBehavior.Implicit,
            options: options)
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
        => resultValue.SetStringValue(runtimeValue.ToString(_localFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TimeOnly runtimeValue)
        => new StringValueNode(runtimeValue.ToString(_localFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out TimeOnly value)
    {
        // Check format.
        if (_enforceSpecFormat && !_localTimeRegex.IsMatch(serialized))
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

    private string GetPattern()
        => _options.InputPrecision == 0
            ? @"^\d{2}:\d{2}:\d{2}$"
            : @"^\d{2}:\d{2}:\d{2}(?:\.\d{1," + _options.InputPrecision + "})?$";

    private string GetLocalFormat()
        => _options.OutputPrecision switch
        {
            DateTimeOptions.DefaultOutputPrecision => LocalFormat,
            0 => "HH:mm:ss",
            _ => $"HH:mm:ss.{new string('F', _options.OutputPrecision)}"
        };

    private Regex GetLocalTimeRegex()
        => _options.InputPrecision switch
        {
            0 => LocalTimeRegex0(),
            1 => LocalTimeRegex1(),
            2 => LocalTimeRegex2(),
            3 => LocalTimeRegex3(),
            4 => LocalTimeRegex4(),
            5 => LocalTimeRegex5(),
            6 => LocalTimeRegex6(),
            _ => LocalTimeRegex7()
        };

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex0();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9])?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex1();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,2})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex2();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,3})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex3();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,4})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex4();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,5})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex5();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,6})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex6();

    [GeneratedRegex(@"^[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?\z", RegexOptions.ExplicitCapture)]
    private static partial Regex LocalTimeRegex7();
}
