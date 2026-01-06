using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NonPositiveFloat scalar type represents a double‐precision fractional value less than or
/// equal to 0.
/// </summary>
public class NonPositiveFloatType : FloatType
{
    /// <summary>
    /// Initializes a new instance of <see cref="NonPositiveFloatType"/>
    /// </summary>
    public NonPositiveFloatType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, double.MinValue, 0, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NonPositiveFloatType"/>
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NonPositiveFloatType()
        : this(
            WellKnownScalarTypes.NonPositiveFloat,
            ScalarResources.NonPositiveFloatType_Description)
    {
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is double d && d <= MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is IFloatValueLiteral floatValueLiteral && floatValueLiteral.ToDouble() <= MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number && inputValue.GetDouble() <= MaxValue;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.NonPositiveFloatType_ParseLiteral_IsNotNonPositive(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NonPositiveFloatType_ParseValue_IsNotNonPositive(this);
}
