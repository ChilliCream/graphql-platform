using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NonPositiveIntType scalar type represents a signed 32-bit numeric non-fractional value
/// less than or equal to 0.
/// </summary>
public class NonPositiveIntType : IntType
{
    /// <summary>
    /// Initializes a new instance of <see cref="NonPositiveIntType"/>
    /// </summary>
    public NonPositiveIntType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, int.MinValue, 0, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NonPositiveIntType"/>
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NonPositiveIntType()
        : this(
            WellKnownScalarTypes.NonPositiveInt,
            ScalarResources.NonPositiveIntType_Description)
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
        => ThrowHelper.NonPositiveIntType_ParseLiteral_IsNotNonPositive(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NonPositiveIntType_ParseValue_IsNotNonPositive(this);
}
