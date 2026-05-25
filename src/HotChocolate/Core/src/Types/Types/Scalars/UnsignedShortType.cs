using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>UnsignedShort</c> scalar type represents an unsigned 16-bit integer. It is intended for
/// scenarios where values are constrained to the range 0 to 65,535, such as representing port
/// numbers, small counts, or other non-negative values that fit within 16 bits.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/unsigned-short.html">Specification</seealso>
public class UnsignedShortType : IntegerTypeBase<ushort>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/unsigned-short.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedShortType"/> class.
    /// </summary>
    public UnsignedShortType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, ushort.MinValue, ushort.MaxValue, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedShortType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedShortType()
        : this(
            ScalarNames.UnsignedShort,
            TypeResources.UnsignedShortType_Description)
    {
    }

    /// <inheritdoc />
    protected override ushort OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToUInt16();

    /// <inheritdoc />
    protected override ushort OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetUInt16();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(ushort runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(ushort runtimeValue)
        => new IntValueNode(runtimeValue);
}
