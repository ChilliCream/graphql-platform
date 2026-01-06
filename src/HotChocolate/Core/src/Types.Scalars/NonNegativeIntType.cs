using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NonNegativeIntType scalar type represents a unsigned 32-bit numeric non-fractional value
/// greater than or equal to 0.
/// </summary>
public class NonNegativeIntType : IntType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonNegativeIntType"/> class.
    /// </summary>
    public NonNegativeIntType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, 0, int.MaxValue, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NonNegativeIntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NonNegativeIntType()
        : this(
            WellKnownScalarTypes.NonNegativeInt,
            ScalarResources.NonNegativeIntType_Description)
    {
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is int i && i >= MinValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is IntValueNode intValueNode && intValueNode.ToInt32() >= MinValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number && inputValue.GetInt32() >= MinValue;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.NonNegativeIntType_ParseLiteral_IsNotNonNegative(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NonNegativeIntType_ParseValue_IsNotNonNegative(this);
}
