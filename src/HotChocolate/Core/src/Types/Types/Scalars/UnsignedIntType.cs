using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>UnsignedInt</c> scalar type represents an unsigned 32-bit integer. It is intended for
/// scenarios where values are constrained to the range 0 to 4,294,967,295, such as representing
/// counts, sizes, indices, or other non-negative integer values.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/unsigned-int.html">Specification</seealso>
public class UnsignedIntType : IntegerTypeBase<uint>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/unsigned-int.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedIntType"/> class.
    /// </summary>
    public UnsignedIntType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, uint.MinValue, uint.MaxValue, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedIntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedIntType()
        : this(
            ScalarNames.UnsignedInt,
            TypeResources.UnsignedIntType_Description)
    {
    }

    /// <inheritdoc />
    protected override uint OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToUInt32();

    /// <inheritdoc />
    protected override uint OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetUInt32();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(uint runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(uint runtimeValue)
        => new IntValueNode(runtimeValue);
}
