using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>Byte</c> scalar type represents a signed 8-bit integer. It is intended for scenarios
/// where values are constrained to the range -128 to 127, such as representing small offsets,
/// temperature differences, or compact signed counters.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/byte.html">Specification</seealso>
public class ByteType : IntegerTypeBase<sbyte>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/byte.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    public ByteType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, sbyte.MinValue, sbyte.MaxValue, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ByteType()
        : this(
            ScalarNames.Byte,
            TypeResources.ByteType_Description)
    {
    }

    /// <inheritdoc />
    protected override sbyte OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToSByte();

    /// <inheritdoc />
    protected override sbyte OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetSByte();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(sbyte runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(sbyte runtimeValue)
        => new IntValueNode(runtimeValue);
}
