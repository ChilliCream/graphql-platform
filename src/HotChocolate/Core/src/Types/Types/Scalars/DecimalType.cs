using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for high-precision decimal numbers in GraphQL.
/// This type serializes as a floating-point number and is suitable for financial calculations
/// where precision is critical.
/// </summary>
public class DecimalType : FloatTypeBase<decimal>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalType"/> class.
    /// </summary>
    public DecimalType(decimal min, decimal max)
        : this(
            ScalarNames.Decimal,
            TypeResources.DecimalType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalType"/> class.
    /// </summary>
    public DecimalType(
        string name,
        string? description = null,
        decimal min = decimal.MinValue,
        decimal max = decimal.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DecimalType()
        : this(decimal.MinValue, decimal.MaxValue)
    {
    }

    /// <inheritdoc />
    protected override decimal OnCoerceInputLiteral(IFloatValueLiteral valueLiteral)
        => valueLiteral.ToDecimal();

    /// <inheritdoc />
    protected override decimal OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetDecimal();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(decimal runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(decimal runtimeValue)
        => new FloatValueNode(runtimeValue);
}
