using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// The Boolean scalar type represents true or false.
/// </para>
/// <para>
/// https://spec.graphql.org/September2025/#sec-Boolean
/// </para>
/// </summary>
public class BooleanType : ScalarType<bool, BooleanValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanType"/> class.
    /// </summary>
    public BooleanType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public BooleanType()
        : this(
            ScalarNames.Boolean,
            TypeResources.BooleanType_Description,
            BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    protected override bool OnCoerceInputLiteral(BooleanValueNode valueLiteral)
        => valueLiteral.Value;

    /// <inheritdoc />
    protected override bool OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => inputValue.ValueKind is JsonValueKind.True;

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(bool runtimeValue, ResultElement resultValue)
        => resultValue.SetBooleanValue(runtimeValue);

    /// <inheritdoc />
    protected override BooleanValueNode OnValueToLiteral(bool runtimeValue)
        => runtimeValue ? BooleanValueNode.True : BooleanValueNode.False;
}
