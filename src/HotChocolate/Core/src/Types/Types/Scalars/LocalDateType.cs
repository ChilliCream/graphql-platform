using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// This scalar represents a date without a time-zone in the
/// <see href="https://en.wikipedia.org/wiki/ISO_8601">ISO-8601</see> calendar system.
/// </para>
/// <para>
/// The pattern is "YYYY-MM-DD" with "YYYY" representing the year, "MM" the month, and "DD" the day.
/// </para>
/// </summary>
/// <seealso href="https://scalars.graphql.org/andimarek/local-date.html">Specification</seealso>
public class LocalDateType : ScalarType<DateOnly, StringValueNode>
{
    private const string LocalFormat = "yyyy-MM-dd";
    private const string SpecifiedByUri = "https://scalars.graphql.org/andimarek/local-date.html";
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
    public override object CoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    public override object CoerceInputValue(JsonElement inputValue)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    public override void CoerceOutputValue(DateOnly runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public override IValueNode ValueToLiteral(DateOnly runtimeValue)
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
