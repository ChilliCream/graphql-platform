using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NegativeIntType scalar type represents a signed 32-bit numeric non-fractional with a
/// maximum of -1.
/// </summary>
public class NegativeIntType : IntType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NegativeIntType"/> class.
    /// </summary>
    public NegativeIntType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, int.MinValue, -1, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NegativeIntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NegativeIntType()
        : this(
            WellKnownScalarTypes.NegativeInt,
            ScalarResources.NegativeIntType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(int runtimeValue)
    {
        return runtimeValue <= MaxValue;
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(IntValueNode valueSyntax)
    {
        return valueSyntax.ToInt32() <= MaxValue;
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        throw ThrowHelper.NegativeIntType_ParseLiteral_IsNotNegative(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        throw ThrowHelper.NegativeIntType_ParseValue_IsNotNegative(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseResultError(object runtimeValue)
    {
        throw ThrowHelper.NegativeIntType_ParseValue_IsNotNegative(this);
    }
}
