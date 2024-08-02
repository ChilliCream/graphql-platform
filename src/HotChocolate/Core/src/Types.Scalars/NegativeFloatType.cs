using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NegativeFloatType scalar represents a double‐precision fractional value less than 0.
/// </summary>
public class NegativeFloatType : FloatType
{
    /// <summary>
    /// Initializes a new instance of <see cref="NegativeFloatType"/>
    /// </summary>
    public NegativeFloatType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, double.MinValue, 0, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NegativeFloatType"/>
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NegativeFloatType()
        : this(
            WellKnownScalarTypes.NegativeFloat,
            ScalarResources.NegativeFloatType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(double runtimeValue)
    {
        return runtimeValue < MaxValue;
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(IFloatValueLiteral valueSyntax)
    {
        return valueSyntax.ToDouble() < MaxValue;
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        throw ThrowHelper.NegativeFloatType_ParseLiteral_IsNotNegative(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        throw ThrowHelper.NegativeFloatType_ParseValue_IsNotNegative(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseResultError(object runtimeValue)
    {
        throw ThrowHelper.NegativeFloatType_ParseValue_IsNotNegative(this);
    }
}
