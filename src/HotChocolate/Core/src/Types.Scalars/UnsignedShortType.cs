using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The UnsignedShortType scalar type represents a unsigned numeric non‐fractional
/// value greater than or equal to 0 and smaller or equal to 65535.
/// </summary>
public class UnsignedShortType : IntegerTypeBase<ushort>
{
    public UnsignedShortType()
        : this(
            WellKnownScalarTypes.UnsignedShort,
            ScalarResources.UnsignedShortType_Description)
    {
    }

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
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        throw ThrowHelper.UnsignedShortType_ParseLiteral_IsNotUnsigned(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        throw ThrowHelper.UnsignedShortType_ParseValue_IsNotUnsigned(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseResultError(object runtimeValue)
    {
        throw ThrowHelper.UnsignedShortType_ParseValue_IsNotUnsigned(this);
    }
}
