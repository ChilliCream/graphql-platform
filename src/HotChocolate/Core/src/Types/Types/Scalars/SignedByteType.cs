using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The SignedByte scalar type represents a signed numeric non‐fractional
/// value greater than or equal to -127 and smaller than or equal to 128.
/// </summary>
public class SignedByteType : IntegerTypeBase<sbyte>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignedByteType"/> class.
    /// </summary>
    public SignedByteType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, sbyte.MinValue, sbyte.MaxValue, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignedByteType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public SignedByteType()
        : this(
            ScalarNames.SignedByte,
            TypeResources.SignedByteType_Description)
    {
    }

    /// <inheritdoc />
    protected override sbyte OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToSByte();

    /// <inheritdoc />
    protected override sbyte OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetSByte();

    /// <inheritdoc />
    public override void OnCoerceOutputValue(sbyte runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    public override IValueNode OnValueToLiteral(sbyte runtimeValue)
        => new IntValueNode(runtimeValue);
}
