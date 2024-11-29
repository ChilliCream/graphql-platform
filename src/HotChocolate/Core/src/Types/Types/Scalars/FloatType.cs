using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The Float scalar type represents signed double‐precision fractional
/// values as specified by IEEE 754. Response formats that support an
/// appropriate double‐precision number type should use that type to
/// represent this scalar.
///
/// http://facebook.github.io/graphql/June2018/#sec-Float
/// </summary>
[SpecScalar]
public class FloatType : FloatTypeBase<double>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatType"/> class.
    /// </summary>
    public FloatType(double min, double max)
        : this(
            ScalarNames.Float,
            TypeResources.FloatType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatType"/> class.
    /// </summary>
    public FloatType(
        string name,
        string? description = null,
        double min = double.MinValue,
        double max = double.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public FloatType()
        : this(double.MinValue, double.MaxValue)
    {
    }

    protected override double ParseLiteral(IFloatValueLiteral valueSyntax) =>
        valueSyntax.ToDouble();

    protected override FloatValueNode ParseValue(double runtimeValue) =>
        new(runtimeValue);
}
