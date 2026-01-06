using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The PositiveInt scalar type represents a signed 32‐bit numeric non‐fractional
/// value of at least the value 1.
/// </summary>
public class PositiveIntType : IntType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PositiveIntType"/> class.
    /// </summary>
    public PositiveIntType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, min: 1, bind: bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PositiveIntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public PositiveIntType()
        : this(
            WellKnownScalarTypes.PositiveInt,
            ScalarResources.PositiveIntType_Description)
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
        => ThrowHelper.PositiveIntType_ParseLiteral_ZeroOrLess(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.PositiveIntType_ParseValue_ZeroOrLess(this);
}
