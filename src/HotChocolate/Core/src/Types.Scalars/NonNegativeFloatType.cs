using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NonNegativeFloatType scalar represents a double‐precision fractional value greater than
/// or equal to 0.
/// </summary>
public class NonNegativeFloatType : FloatType
{
    /// <summary>
    /// Initializes a new instance of <see cref="NonNegativeFloatType"/>
    /// </summary>
    public NonNegativeFloatType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, 0, double.MaxValue, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NonNegativeFloatType"/>
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NonNegativeFloatType()
        : this(
            WellKnownScalarTypes.NonNegativeFloat,
            ScalarResources.NonNegativeFloatType_Description)
    {
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is double d && d >= MinValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is IFloatValueLiteral floatValueLiteral && floatValueLiteral.ToDouble() >= MinValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number && inputValue.GetDouble() >= MinValue;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.NonNegativeFloatType_ParseLiteral_IsNotNonNegative(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NonNegativeFloatType_ParseValue_IsNotNonNegative(this);
}
