using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The `UnsignedLong` scalar type represents a signed 64‐bit numeric non‐fractional
/// value greater than or equal to 0.
/// </summary>
public class UnsignedLongType : IntegerTypeBase<ulong>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedLongType"/> class.
    /// </summary>
    public UnsignedLongType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, ulong.MinValue, ulong.MaxValue, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedLongType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedLongType()
        : this(
            ScalarNames.UnsignedLong,
            TypeResources.UnsignedLongType_Description)
    {
    }

    /// <inheritdoc />
    protected override ulong OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToUInt64();

    /// <inheritdoc />
    protected override ulong OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetUInt64();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(ulong runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(ulong runtimeValue)
        => new IntValueNode(runtimeValue);
}
