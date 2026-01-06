using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NegativeFloatType scalar represents a double‐precision fractional value less than 0.
/// </summary>
public class NegativeFloatType : FloatType
{
    /// <summary>
    /// Initializes a new instance of <see cref="NegativeFloatType"/>
    /// </summary>
    public NegativeFloatType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, double.MinValue, 0, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NegativeFloatType"/>
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NegativeFloatType()
        : this(
            WellKnownScalarTypes.NegativeFloat,
            ScalarResources.NegativeFloatType_Description)
    {
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is double d && d < MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is IFloatValueLiteral floatValueLiteral && floatValueLiteral.ToDouble() < MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number && inputValue.GetDouble() < MaxValue;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.NegativeFloatType_ParseLiteral_IsNotNegative(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NegativeFloatType_ParseValue_IsNotNegative(this);
}
