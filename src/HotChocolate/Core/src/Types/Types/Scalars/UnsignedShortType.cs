using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The UnsignedShortType scalar type represents an unsigned numeric non‐fractional
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
            ScalarNames.UnsignedShort,
            TypeResources.UnsignedShortType_Description)
    {
    }

    /// <inheritdoc />
    protected override ushort OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToUInt16();

    /// <inheritdoc />
    protected override ushort OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetUInt16();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(ushort runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(ushort runtimeValue)
        => new IntValueNode(runtimeValue);
}
