using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `Port` scalar type represents a field whose value is a valid TCP port within the
/// range of 0 to 65535 as defined here:
/// <a href="https://en.wikipedia.org/wiki/Transmission_Control_Protocol#TCP_ports">
/// TCP ports
/// </a>
/// </summary>
public class PortType : IntType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PortType"/> class.
    /// </summary>
    public PortType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, 0, 65535, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PortType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public PortType()
        : this(
            WellKnownScalarTypes.Port,
            ScalarResources.PortType_Description)
    {
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is int i && i >= MinValue && i <= MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is IntValueNode intValueNode && intValueNode.ToInt32() >= MinValue && intValueNode.ToInt32() <= MaxValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number && inputValue.GetInt32() >= MinValue && inputValue.GetInt32() <= MaxValue;

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => ThrowHelper.PortType_ParseLiteral_OutOfRange(this);

    /// <inheritdoc />
    protected override LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => ThrowHelper.PortType_ParseValue_OutOfRange(this);
}
