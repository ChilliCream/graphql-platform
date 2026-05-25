using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>UnsignedByte</c> scalar type represents an unsigned 8-bit integer. It is intended for
/// scenarios where values are constrained to the range 0 to 255, such as representing color channel
/// values (RGB), small counters, or byte-level data.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/unsigned-byte.html">Specification</seealso>
public class UnsignedByteType : IntegerTypeBase<byte>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/unsigned-byte.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedByteType"/> class.
    /// </summary>
    public UnsignedByteType(byte min, byte max)
        : this(
            ScalarNames.UnsignedByte,
            TypeResources.UnsignedByteType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedByteType"/> class.
    /// </summary>
    public UnsignedByteType(
        string name,
        string? description = null,
        byte min = byte.MinValue,
        byte max = byte.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedByteType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedByteType()
        : this(byte.MinValue, byte.MaxValue)
    {
    }

    protected override byte OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToByte();

    protected override byte OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetByte();

    protected override void OnCoerceOutputValue(byte runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    protected override IValueNode OnValueToLiteral(byte runtimeValue)
        => new IntValueNode(runtimeValue);
}
