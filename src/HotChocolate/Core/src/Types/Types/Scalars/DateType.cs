using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

public class DateType : ScalarType<DateOnly, StringValueNode>
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly bool _enforceSpecFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateType"/> class.
    /// </summary>
    public DateType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool disableFormatCheck = false)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^\d{4}-\d{2}-\d{2}$";
        _enforceSpecFormat = !disableFormatCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateType"/> class.
    /// </summary>
    public DateType(bool disableFormatCheck)
        : this(
            ScalarNames.Date,
            TypeResources.DateType_Description,
            BindingBehavior.Implicit,
            disableFormatCheck: disableFormatCheck)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DateType() : this(ScalarNames.Date, TypeResources.DateType_Description)
    {
    }

    public override object CoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value.Value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    public override object CoerceInputValue(JsonElement inputValue)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value.Value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    public override void CoerceOutputValue(DateOnly runtimeValue, ResultElement resultValue)
    {
        var serialized = runtimeValue.ToString(DateFormat, CultureInfo.InvariantCulture);
        resultValue.SetStringValue(serialized);
    }

    public override IValueNode ValueToLiteral(DateOnly runtimeValue)
    {
        var serialized = runtimeValue.ToString(DateFormat, CultureInfo.InvariantCulture);
        return new StringValueNode(serialized);
    }

    private bool TryParseStringValue(
        string serialized,
        [NotNullWhen(true)] out DateOnly? value)
    {
        if (_enforceSpecFormat)
        {
            if (DateOnly.TryParseExact(
                serialized,
                DateFormat,
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

        value = null;
        return false;
    }
}
