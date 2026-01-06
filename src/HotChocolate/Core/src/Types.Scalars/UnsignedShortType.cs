using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The UnsignedShortType scalar type represents a unsigned numeric non‐fractional
/// value greater than or equal to 0 and smaller or equal to 65535.
/// </summary>
public class UnsignedShortType : IntegerTypeBase<ushort>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedShortType"/> class.
    /// </summary>
    public UnsignedShortType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, ushort.MinValue, ushort.MaxValue, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedShortType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedShortType()
        : this(
            WellKnownScalarTypes.UnsignedShort,
            ScalarResources.UnsignedShortType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(ushort runtimeValue)
    {
        return runtimeValue >= MinValue;
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(IntValueNode valueSyntax)
    {
        return valueSyntax.ToUInt16() >= MinValue;
    }

    /// <inheritdoc />
    protected override ushort ParseLiteral(IntValueNode valueSyntax)
    {
        return valueSyntax.ToUInt16();
    }

    /// <inheritdoc />
    protected override IntValueNode ParseValue(ushort runtimeValue)
    {
        return new(runtimeValue);
    }

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.UnsignedShortType_ParseLiteral_IsNotUnsigned(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateParseValueError(object runtimeValue)
        => ThrowHelper.UnsignedShortType_ParseValue_IsNotUnsigned(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateParseResultError(object runtimeValue)
        => ThrowHelper.UnsignedShortType_ParseValue_IsNotUnsigned(this);
}
