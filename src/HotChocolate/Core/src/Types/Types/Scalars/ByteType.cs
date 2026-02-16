using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The Byte scalar type represents a signed numeric non‐fractional
/// value greater than or equal to -128 and smaller than or equal to 127.
/// </summary>
public class ByteType : IntegerTypeBase<sbyte>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    public ByteType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, sbyte.MinValue, sbyte.MaxValue, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ByteType()
        : this(
            ScalarNames.Byte,
            TypeResources.ByteType_Description)
    {
    }

    /// <inheritdoc />
    protected override sbyte OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToSByte();

    /// <inheritdoc />
    protected override sbyte OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetSByte();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(sbyte runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(sbyte runtimeValue)
        => new IntValueNode(runtimeValue);
}
