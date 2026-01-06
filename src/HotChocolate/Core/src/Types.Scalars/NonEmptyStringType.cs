using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NonEmptyString scalar type represents non-empty textual data, represented as
/// UTF‐8 character sequences with at least one character
/// </summary>
public class NonEmptyStringType : StringType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonEmptyStringType"/> class.
    /// </summary>
    public NonEmptyStringType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NonEmptyStringType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NonEmptyStringType()
        : this(
            WellKnownScalarTypes.NonEmptyString,
            ScalarResources.NonEmptyStringType_Description)
    {
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is string s && s != string.Empty;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is StringValueNode stringValueNode && stringValueNode.Value != string.Empty;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.String && inputValue.GetString() != string.Empty;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.NonEmptyStringType_ParseLiteral_IsEmpty(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.NonEmptyStringType_ParseValue_IsEmpty(this);
}
