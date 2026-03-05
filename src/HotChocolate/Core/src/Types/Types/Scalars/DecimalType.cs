using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>Decimal</c> scalar type represents a decimal floating-point number with high precision.
/// It is intended for scenarios where precise decimal representation is critical, such as financial
/// calculations, monetary values, scientific measurements, or any domain where floating-point
/// rounding errors are unacceptable.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/decimal.html">Specification</seealso>
public class DecimalType : FloatTypeBase<decimal>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/decimal.html";

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
        SpecifiedBy = new Uri(SpecifiedByUri);
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
