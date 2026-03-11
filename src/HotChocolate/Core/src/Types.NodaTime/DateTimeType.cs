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
/// The <c>DateTime</c> scalar type represents a date and time with time zone offset information. It
/// is intended for scenarios where representing a specific instant in time is required, such as
/// recording when an event occurred, scheduling future events across time zones, or storing
/// timestamps for auditing purposes.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/date-time.html">Specification</seealso>
public class DateTimeType : ScalarType<OffsetDateTime, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/date-time.html";

    private readonly DateTimeOptions _options;
    private readonly OffsetDateTimePattern _inputPattern;
    private readonly string _outputFormat;

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
        _inputPattern = OffsetDateTimePattern.CreateWithInvariantCulture(GetInputFormat());
        _outputFormat = GetOutputFormat();
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

    protected override OffsetDateTime OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    protected override OffsetDateTime OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    protected override void OnCoerceOutputValue(OffsetDateTime runtimeValue, ResultElement resultValue)
    {
        resultValue.SetStringValue(
            runtimeValue.ToString(
                _outputFormat,
                CultureInfo.InvariantCulture));
    }

    protected override StringValueNode OnValueToLiteral(OffsetDateTime runtimeValue)
    {
        return new StringValueNode(
            runtimeValue.ToString(
                _outputFormat,
                CultureInfo.InvariantCulture));
    }

    private bool TryParseStringValue(string serialized, out OffsetDateTime value)
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
            ? @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:[Zz]|[+-]\d{2}:\d{2})$"
            : @"^\d{4}-\d{2}-\d{2}[Tt]\d{2}:\d{2}:\d{2}(?:\.\d{1,"
                + _options.InputPrecision
                + @"})?(?:[Zz]|[+-]\d{2}:\d{2})$";

    private string GetInputFormat()
        => _options.InputPrecision switch
        {
            0 => "uuuu-MM-dd'T'HH:mm:sso<Z+HH:mm>",
            _ => $"uuuu-MM-dd'T'HH:mm:ss.{new string('F', _options.InputPrecision)}o<Z+HH:mm>"
        };

    private string GetOutputFormat()
        => _options.OutputPrecision switch
        {
            0 => "uuuu-MM-dd'T'HH:mm:sso<Z+HH:mm>",
            _ => $"uuuu-MM-dd'T'HH:mm:ss.{new string('F', _options.OutputPrecision)}o<Z+HH:mm>"
        };
}
