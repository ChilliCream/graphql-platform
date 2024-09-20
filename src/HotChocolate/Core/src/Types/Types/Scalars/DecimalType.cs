using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

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

    protected override decimal ParseLiteral(IFloatValueLiteral valueSyntax) =>
        valueSyntax.ToDecimal();

    protected override FloatValueNode ParseValue(decimal runtimeValue) =>
        new(runtimeValue);
}
