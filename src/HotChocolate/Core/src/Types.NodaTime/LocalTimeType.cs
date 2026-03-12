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
/// The <c>LocalTime</c> scalar type represents a time of day without date or time zone information.
/// It is intended for scenarios where only the time component matters, such as business operating
/// hours (e.g., "opens at 09:00"), daily schedules, or recurring time-based events where the
/// specific date is irrelevant.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/local-time.html">Specification</seealso>
public class LocalTimeType : ScalarType<LocalTime, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/local-time.html";

    private readonly DateTimeOptions _options;
    private readonly LocalTimePattern _inputPattern;
    private readonly string _outputFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTimeType"/> class.
    /// </summary>
    public LocalTimeType(
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
        _inputPattern = LocalTimePattern.CreateWithInvariantCulture(GetFormat(_options.InputPrecision));
        _outputFormat = GetFormat(_options.OutputPrecision);
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
            TypeResources.LocalTimeType_Description,
            options: null)
    {
    }

    /// <inheritdoc />
    protected override LocalTime OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override LocalTime OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(LocalTime runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(_outputFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(LocalTime runtimeValue)
        => new StringValueNode(runtimeValue.ToString(_outputFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out LocalTime value)
    {
        var result = _inputPattern.Parse(serialized);

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
            ? @"^\d{2}:\d{2}:\d{2}$"
            : @"^\d{2}:\d{2}:\d{2}(?:\.\d{1," + _options.InputPrecision + "})?$";

    private static string GetFormat(byte precision)
        => precision == 0
            ? "HH:mm:ss"
            : $"HH:mm:ss.{new string('F', precision)}";
}
