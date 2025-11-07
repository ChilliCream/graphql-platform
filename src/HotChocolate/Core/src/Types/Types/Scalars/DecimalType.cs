using HotChocolate.Language;
using HotChocolate.Properties;

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
        SerializationType = ScalarSerializationType.Float;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public DecimalType()
        : this(decimal.MinValue, decimal.MaxValue)
    {
    }

    protected override decimal ParseLiteral(IFloatValueLiteral valueSyntax)
        => valueSyntax.ToDecimal();

    protected override FloatValueNode ParseValue(decimal runtimeValue)
        => new(runtimeValue);
}
