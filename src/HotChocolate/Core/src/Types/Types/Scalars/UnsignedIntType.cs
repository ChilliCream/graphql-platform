using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The UnsignedInt scalar type represents an unsigned 32‐bit numeric non‐fractional
/// value greater than or equal to 0.
/// </summary>
public class UnsignedIntType : IntegerTypeBase<uint>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedIntType"/> class.
    /// </summary>
    public UnsignedIntType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, uint.MinValue, uint.MaxValue, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedIntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedIntType()
        : this(
            ScalarNames.UnsignedInt,
            TypeResources.UnsignedIntType_Description)
    {
    }

    /// <inheritdoc />
    protected override uint OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToUInt32();

    /// <inheritdoc />
    protected override uint OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetUInt32();

    /// <inheritdoc />
    public override void OnCoerceOutputValue(uint runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    public override IValueNode OnValueToLiteral(uint runtimeValue)
        => new IntValueNode(runtimeValue);
}
