using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for unsigned 8-bit integers (byte) in GraphQL.
/// This type serializes as an integer and supports values from 0 to 255.
/// </summary>
public class ByteType : IntegerTypeBase<byte>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    public ByteType(byte min, byte max)
        : this(
            ScalarNames.Byte,
            TypeResources.ByteType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    public ByteType(
        string name,
        string? description = null,
        byte min = byte.MinValue,
        byte max = byte.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
        SerializationType = ScalarSerializationType.Int;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ByteType()
        : this(byte.MinValue, byte.MaxValue)
    {
    }

    protected override byte ParseLiteral(IntValueNode valueSyntax) =>
        valueSyntax.ToByte();

    protected override IntValueNode ParseValue(byte runtimeValue) =>
        new(runtimeValue);
}
