using System.Globalization;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The <c>LocalDate</c> scalar type represents a date without time or time zone information. It is
/// intended for scenarios where only the calendar date matters in a local context, such as contract
/// effective dates, publication dates, or recurring events (e.g., "New Year's Day is January 1st"),
/// where the specific time of day and time zone are irrelevant or managed separately.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/local-date.html">Specification</seealso>
public class LocalDateType : ScalarType<DateOnly, StringValueNode>
{
    private const string LocalFormat = "yyyy-MM-dd";
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/local-date.html";
    private readonly bool _enforceSpecFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    public LocalDateType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^\d{4}-\d{2}-\d{2}$";
        SpecifiedBy = new Uri(SpecifiedByUri);
        _enforceSpecFormat = !disableFormatCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateType"/> class.
    /// </summary>
    public LocalDateType(bool disableFormatCheck)
        : this(
            ScalarNames.LocalDate,
            TypeResources.LocalDateType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
    {
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
    protected override DateOnly OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override DateOnly OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(DateOnly runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(DateOnly runtimeValue)
        => new StringValueNode(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    private bool TryParseStringValue(string serialized, out DateOnly value)
    {
        if (_enforceSpecFormat)
        {
            if (DateOnly.TryParseExact(
                serialized,
                LocalFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                value = date;
                return true;
            }
        }
        else if (DateOnly.TryParse(
            serialized,
            CultureInfo.InvariantCulture,
            out var date))
        {
            value = date;
            return true;
        }

        value = default;
        return false;
    }
}
