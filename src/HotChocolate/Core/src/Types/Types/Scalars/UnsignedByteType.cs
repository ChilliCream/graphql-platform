using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for unsigned 8-bit integers (byte) in GraphQL.
/// This type serializes as an integer and supports values from 0 to 255.
/// </summary>
public class UnsignedByteType : IntegerTypeBase<byte>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedByteType"/> class.
    /// </summary>
    public UnsignedByteType(byte min, byte max)
        : this(
            ScalarNames.UnsignedByte,
            TypeResources.UnsignedByteType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedByteType"/> class.
    /// </summary>
    public UnsignedByteType(
        string name,
        string? description = null,
        byte min = byte.MinValue,
        byte max = byte.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedByteType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedByteType()
        : this(byte.MinValue, byte.MaxValue)
    {
    }

    protected override byte OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToByte();

    protected override byte OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetByte();

    protected override void OnCoerceOutputValue(byte runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    protected override IValueNode OnValueToLiteral(byte runtimeValue)
        => new IntValueNode(runtimeValue);
}
