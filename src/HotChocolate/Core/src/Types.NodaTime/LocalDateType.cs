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
/// The <c>LocalDate</c> scalar type represents a date without time or time zone information. It is
/// intended for scenarios where only the calendar date matters in a local context, such as contract
/// effective dates, publication dates, or recurring events (e.g., "New Year's Day is January 1st"),
/// where the specific time of day and time zone are irrelevant or managed separately.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/local-date.html">Specification</seealso>
public class LocalDateType : ScalarType<LocalDate, StringValueNode>
{
    private const string LocalFormat = "uuuu-MM-dd";
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/local-date.html";

    private readonly LocalDatePattern _inputPattern;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    public LocalDateType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^\d{4}-\d{2}-\d{2}$";
        SpecifiedBy = new Uri(SpecifiedByUri);
        _inputPattern = LocalDatePattern.CreateWithInvariantCulture(LocalFormat);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalDateType()
        : this(
            ScalarNames.LocalDate,
            TypeResources.LocalDateType_Description)
    {
    }

    /// <inheritdoc />
    protected override LocalDate OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override LocalDate OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(LocalDate runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(LocalDate runtimeValue)
        => new StringValueNode(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out LocalDate value)
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
}
