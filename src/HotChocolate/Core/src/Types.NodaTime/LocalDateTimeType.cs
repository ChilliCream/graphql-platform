using System.Globalization;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using NodaTime;
using NodaTime.Text;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// The <c>LocalDateTime</c> scalar type represents a date and time without time zone information.
/// It is intended for scenarios where time zone context is either unnecessary or managed
/// separately, such as recording birthdates and times (where the event occurred in a specific local
/// context), displaying timestamps in a user's local time zone (where the time zone is known from
/// context), or recording historical timestamps where the time zone was not captured.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/local-date-time.html">Specification</seealso>
public class LocalDateTimeType : ScalarType<LocalDateTime, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/local-date-time.html";

    private readonly DateTimeOptions _options;
    private readonly LocalDateTimePattern _inputPattern;
    private readonly string _outputFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    public LocalDateTimeType(
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
        _inputPattern = LocalDateTimePattern.CreateWithInvariantCulture(GetFormat(_options.InputPrecision));
        _outputFormat = GetFormat(_options.OutputPrecision);
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
            TypeResources.LocalDateTimeType_Description,
            options: null)
    {
    }

    /// <inheritdoc />
    protected override LocalDateTime OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override LocalDateTime OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(LocalDateTime runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(_outputFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(LocalDateTime runtimeValue)
        => new StringValueNode(runtimeValue.ToString(_outputFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out LocalDateTime value)
    {
        var result = _inputPattern.Parse(serialized.Replace('t', 'T'));

        if (result.Success)
        {
            value = result.Value;
            return true;
        }

        value = default;
        return false;
    }

    private string GetPattern()
        => _options.InputPrecision == 0
            ? @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}$"
            : @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1," + _options.InputPrecision + "})?$";

    private static string GetFormat(byte precision)
        => precision == 0
            ? @"uuuu-MM-dd'T'HH\:mm\:ss"
            : @$"uuuu-MM-dd'T'HH\:mm\:ss.{new string('F', precision)}";
}
