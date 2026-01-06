using System.Text.Json;
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
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is int i && i <= MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is IntValueNode intValueNode && intValueNode.ToInt32() <= MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number && inputValue.GetInt32() <= MaxValue;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.NegativeIntType_ParseLiteral_IsNotNegative(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NegativeIntType_ParseValue_IsNotNegative(this);
}
