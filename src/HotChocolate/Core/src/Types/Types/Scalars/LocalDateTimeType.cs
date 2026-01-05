using System.Globalization;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The `LocalDateTime` scalar type is a local date/time string (i.e., with no associated timezone)
/// with the format `YYYY-MM-DDThh:mm:ss`.
/// </summary>
public class LocalDateTimeType : ScalarType<DateTime, StringValueNode>
{
    private const string LocalFormat = "yyyy-MM-ddTHH\\:mm\\:ss";

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalDateTimeType"/> class.
    /// </summary>
    public LocalDateTimeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$";
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
    public override object CoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseStringValue(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    public override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseStringValue(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    public override void CoerceOutputValue(DateTime runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public override IValueNode ValueToLiteral(DateTime runtimeValue)
        => new StringValueNode(runtimeValue.ToString(LocalFormat, CultureInfo.InvariantCulture));

    private static bool TryParseStringValue(string serialized, out DateTime value)
    {
        if (DateTime.TryParseExact(
            serialized,
            LocalFormat,
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
}
